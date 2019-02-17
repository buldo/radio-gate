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
        readonly TcpClient _client;
        readonly IPEndPoint _host;

        NetworkStream _netStream;
        SslStream _ssl;
        BinaryReader _reader;
        BinaryWriter _writer;

        readonly IMumbleProtocol _protocol;
        readonly MumbleConnection _connection;

        public TcpSocket(IPEndPoint host, IMumbleProtocol protocol, MumbleConnection connection)
        {
            _host = host;
            _protocol = protocol;
            _connection = connection;
            _client = new TcpClient();
        }

        private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors errors)
        {
            return _protocol.ValidateCertificate(sender, certificate, chain, errors);
        }

        private X509Certificate SelectCertificate(object sender, string targetHost,
            X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return _protocol.SelectCertificate(sender, targetHost, localCertificates, remoteCertificate,
                acceptableIssuers);
        }

        public void Connect(string username, string password, string[] tokens, string serverName)
        {
            _client.Connect(_host);

            _netStream = _client.GetStream();
            _ssl = new SslStream(_netStream, false, ValidateCertificate, SelectCertificate);
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

                Serializer.SerializeWithLengthPrefix<T>(_ssl, packet, PrefixStyle.Fixed32BigEndian);
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

            lock (_ssl)
            {
                PacketType type = (PacketType) IPAddress.NetworkToHostOrder(_reader.ReadInt16());
                Console.WriteLine("{0:HH:mm:ss}: {1}", DateTime.Now, type.ToString());

                switch (type)
                {
                    case PacketType.Version:
                        _protocol.Version(
                            Serializer.DeserializeWithLengthPrefix<MumbleProto.Version>(_ssl,
                                PrefixStyle.Fixed32BigEndian));
                        break;
                    case PacketType.CryptSetup:
                        var cryptSetup =
                            Serializer.DeserializeWithLengthPrefix<CryptSetup>(_ssl, PrefixStyle.Fixed32BigEndian);
                        _connection.ProcessCryptState(cryptSetup);
                        SendPing();
                        break;
                    case PacketType.ChannelState:
                        _protocol.ChannelState(
                            Serializer.DeserializeWithLengthPrefix<ChannelState>(_ssl, PrefixStyle.Fixed32BigEndian));
                        break;
                    case PacketType.UserState:
                        _protocol.UserState(
                            Serializer.DeserializeWithLengthPrefix<UserState>(_ssl, PrefixStyle.Fixed32BigEndian));
                        break;
                    case PacketType.CodecVersion:
                        _protocol.CodecVersion(
                            Serializer.DeserializeWithLengthPrefix<CodecVersion>(_ssl, PrefixStyle.Fixed32BigEndian));
                        break;
                    case PacketType.ContextAction:
                        _protocol.ContextAction(
                            Serializer.DeserializeWithLengthPrefix<ContextAction>(_ssl, PrefixStyle.Fixed32BigEndian));
                        break;
                    case PacketType.ContextActionModify:
                        _protocol.ContextActionModify(
                            Serializer.DeserializeWithLengthPrefix<ContextActionModify>(_ssl,
                                PrefixStyle.Fixed32BigEndian));
                        break;
                    case PacketType.PermissionQuery:
                        _protocol.PermissionQuery(
                            Serializer.DeserializeWithLengthPrefix<PermissionQuery>(_ssl,
                                PrefixStyle.Fixed32BigEndian));
                        break;
                    case PacketType.ServerSync:
                        _protocol.ServerSync(
                            Serializer.DeserializeWithLengthPrefix<ServerSync>(_ssl, PrefixStyle.Fixed32BigEndian));
                        break;
                    case PacketType.ServerConfig:
                        _protocol.ServerConfig(
                            Serializer.DeserializeWithLengthPrefix<ServerConfig>(_ssl, PrefixStyle.Fixed32BigEndian));
                        break;
                    case PacketType.UDPTunnel:
                        var length = IPAddress.NetworkToHostOrder(_reader.ReadInt32());
                        _connection.ReceiveDecryptedUdp(_reader.ReadBytes(length));
                        break;
                    case PacketType.Ping:
                        var ping = Serializer.DeserializeWithLengthPrefix<Ping>(_ssl, PrefixStyle.Fixed32BigEndian);
                        _connection._pingProcessor.ReceivePing(ping);
                        _protocol.Ping(ping);
                        break;
                    case PacketType.UserRemove:
                        _protocol.UserRemove(
                            Serializer.DeserializeWithLengthPrefix<UserRemove>(_ssl, PrefixStyle.Fixed32BigEndian));
                        break;
                    case PacketType.ChannelRemove:
                        _protocol.ChannelRemove(
                            Serializer.DeserializeWithLengthPrefix<ChannelRemove>(_ssl, PrefixStyle.Fixed32BigEndian));
                        break;
                    case PacketType.TextMessage:
                        var message =
                            Serializer.DeserializeWithLengthPrefix<TextMessage>(_ssl, PrefixStyle.Fixed32BigEndian);
                        _protocol.TextMessage(message);
                        break;

                    case PacketType.Reject:
                        throw new NotImplementedException();

                    case PacketType.UserList:
                        _protocol.UserList(
                            Serializer.DeserializeWithLengthPrefix<UserList>(_ssl, PrefixStyle.Fixed32BigEndian));
                        break;

                    case PacketType.SuggestConfig:
                        _protocol.SuggestConfig(
                            Serializer.DeserializeWithLengthPrefix<SuggestConfig>(_ssl, PrefixStyle.Fixed32BigEndian));
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
        }
    }
}
