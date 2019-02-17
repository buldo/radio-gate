using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using MumbleProto;
using MumbleSharp.Packets;
using ProtoBuf;

namespace MumbleSharp
{
    internal class TcpSocket
    {
        private const PrefixStyle serializationStyle = PrefixStyle.Fixed32BigEndian;
        readonly TcpClient _client;
        readonly IPEndPoint _host;

        NetworkStream _netStream;
        SslStream _ssl;
        BinaryReader _reader;
        BinaryWriter _writer;

        readonly MumbleConnection _connection;

        public TcpSocket(IPEndPoint host, MumbleConnection connection)
        {
            _host = host;
            _connection = connection;
            _client = new TcpClient();
        }

        public event EventHandler<PacketReceivedEventArgs> PacketReceived;

        public void Connect(
            string username,
            string password,
            string[] tokens,
            string serverName,
            RemoteCertificateValidationCallback validateCertificate,
            LocalCertificateSelectionCallback selectCertificate)
        {
            _client.Connect(_host);

            _netStream = _client.GetStream();
            _ssl = new SslStream(_netStream, false, validateCertificate, selectCertificate);
            _ssl.AuthenticateAsClient(serverName);
            _reader = new BinaryReader(_ssl);
            _writer = new BinaryWriter(_ssl);

            DateTime startWait = DateTime.Now;
            while (!_ssl.IsAuthenticated)
            {
                if (DateTime.Now - startWait > TimeSpan.FromSeconds(2))
                    throw new TimeoutException("Timed out waiting for ssl authentication");

                System.Threading.Thread.Sleep(10);
            }

            Handshake(username, password, tokens);
        }

        public void Close()
        {
            _reader.Close();
            _writer.Close();
            _ssl = null;
            _netStream.Close();
            _client.Close();
        }

        private void Handshake(string username, string password, string[] tokens)
        {
            MumbleProto.Version version = new MumbleProto.Version
            {
                Release = "MumbleSharp",
                version = (1 << 16) | (2 << 8) | (0 & 0xFF),
                Os = Environment.OSVersion.ToString(),
                OsVersion = Environment.OSVersion.VersionString,
            };
            Send(PacketType.Version, version);

            Authenticate auth = new Authenticate
            {
                Username = username,
                Password = password,
                Opus = true,
            };
            auth.Tokens.AddRange(tokens ?? new string[0]);
            auth.CeltVersions = new int[] {unchecked((int) 0x8000000b)};

            Send(PacketType.Authenticate, auth);
        }

        public void Send<T>(PacketType type, T packet)
        {
            lock (_ssl)
            {
                _writer.Write(IPAddress.HostToNetworkOrder((short) type));
                _writer.Flush();

                Serializer.SerializeWithLengthPrefix<T>(_ssl, packet, serializationStyle);
                _ssl.Flush();
                _netStream.Flush();
            }
        }

        public void Send(PacketType type, ArraySegment<byte> packet)
        {
            lock (_ssl)
            {
                _writer.Write(IPAddress.HostToNetworkOrder((short) type));
                _writer.Write(IPAddress.HostToNetworkOrder(packet.Count));
                _writer.Write(packet.Array, packet.Offset, packet.Count);

                _writer.Flush();
                _ssl.Flush();
                _netStream.Flush();
            }
        }

        public void SendVoice(PacketType type, ArraySegment<byte> packet)
        {
            lock (_ssl)
            {
                _writer.Write(IPAddress.HostToNetworkOrder((short) type));
                _writer.Write(IPAddress.HostToNetworkOrder(packet.Count));
                _writer.Write(packet.Array, packet.Offset, packet.Count);

                _writer.Flush();
                _ssl.Flush();
                _netStream.Flush();
            }
        }

        public void SendBuffer(PacketType type, byte[] packet)
        {
            lock (_ssl)
            {
                _writer.Write(IPAddress.HostToNetworkOrder((short) type));
                _writer.Write(IPAddress.HostToNetworkOrder(packet.Length));
                _writer.Write(packet, 0, packet.Length);

                _writer.Flush();
                _ssl.Flush();
                _netStream.Flush();
            }
        }

        public void SendPing()
        {
            var ping = _connection._pingProcessor.CreateTcpPing();

            lock (_ssl)
                Send<Ping>(PacketType.Ping, ping);
        }

        public void Process()
        {
            if (!_client.Connected)
                throw new InvalidOperationException("Not connected");

            if (!_netStream.DataAvailable)
                return;

            PacketType type;
            object packet;
            lock (_ssl)
            {
                type = (PacketType) IPAddress.NetworkToHostOrder(_reader.ReadInt16());
                Console.WriteLine("{0:HH:mm:ss}: {1}", DateTime.Now, type.ToString());

                switch (type)
                {
                    case PacketType.Version:
                        packet = Serializer.DeserializeWithLengthPrefix<MumbleProto.Version>(_ssl, serializationStyle);
                        break;
                    case PacketType.CryptSetup:
                        packet = Serializer.DeserializeWithLengthPrefix<CryptSetup>(_ssl, serializationStyle);
                        break;
                    case PacketType.ChannelState:
                        packet = Serializer.DeserializeWithLengthPrefix<ChannelState>(_ssl, serializationStyle);
                        break;
                    case PacketType.UserState:
                        packet = Serializer.DeserializeWithLengthPrefix<UserState>(_ssl, serializationStyle);
                        break;
                    case PacketType.CodecVersion:
                        packet = Serializer.DeserializeWithLengthPrefix<CodecVersion>(_ssl, serializationStyle);
                        break;
                    case PacketType.ContextAction:
                        packet = Serializer.DeserializeWithLengthPrefix<ContextAction>(_ssl, serializationStyle);
                        break;
                    case PacketType.ContextActionModify:
                        packet = Serializer.DeserializeWithLengthPrefix<ContextActionModify>(_ssl, serializationStyle);
                        break;
                    case PacketType.PermissionQuery:
                        packet = Serializer.DeserializeWithLengthPrefix<PermissionQuery>(_ssl, serializationStyle);
                        break;
                    case PacketType.ServerSync:
                        packet = Serializer.DeserializeWithLengthPrefix<ServerSync>(_ssl, serializationStyle);
                        break;
                    case PacketType.ServerConfig:
                        packet = Serializer.DeserializeWithLengthPrefix<ServerConfig>(_ssl, serializationStyle);
                        break;
                    case PacketType.UDPTunnel:
                        var length = IPAddress.NetworkToHostOrder(_reader.ReadInt32());
                        packet = _reader.ReadBytes(length);
                        break;
                    case PacketType.Ping:
                        packet = Serializer.DeserializeWithLengthPrefix<Ping>(_ssl, serializationStyle);
                        break;
                    case PacketType.UserRemove:
                        packet = Serializer.DeserializeWithLengthPrefix<UserRemove>(_ssl, serializationStyle);
                        break;
                    case PacketType.ChannelRemove:
                        packet = Serializer.DeserializeWithLengthPrefix<ChannelRemove>(_ssl, serializationStyle);
                        break;
                    case PacketType.TextMessage:
                        packet = Serializer.DeserializeWithLengthPrefix<TextMessage>(_ssl, serializationStyle);
                        break;

                    case PacketType.Reject:
                        throw new NotImplementedException();

                    case PacketType.UserList:
                        packet = Serializer.DeserializeWithLengthPrefix<UserList>(_ssl, serializationStyle);
                        break;

                    case PacketType.SuggestConfig:
                        packet = Serializer.DeserializeWithLengthPrefix<SuggestConfig>(_ssl, serializationStyle);
                        break;

                    case PacketType.Authenticate:
                    case PacketType.PermissionDenied:
                    case PacketType.ACL:
                    case PacketType.QueryUsers:
                    case PacketType.VoiceTarget:
                    case PacketType.UserStats:
                    case PacketType.RequestBlob:
                    case PacketType.BanList:
                    default:
                        throw new NotImplementedException();
                }
            }

            OnPacketReceived(type, packet);
        }

        private void OnPacketReceived(PacketType packetType, object packet)
        {
            var eventArgs = new PacketReceivedEventArgs(packetType, packet);
            PacketReceived?.Invoke(this, eventArgs);
        }
    }
}
