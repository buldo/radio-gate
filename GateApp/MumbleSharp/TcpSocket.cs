using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MumbleProto;
using MumbleSharp.Packets;
using ProtoBuf;
using ProtoBuf.Meta;

namespace MumbleSharp
{
    internal class TcpSocket
    {
        private const PrefixStyle SerializationStyle = PrefixStyle.Fixed32BigEndian;
        private readonly ILogger _logger;

        readonly TcpClient _client;
        readonly IPEndPoint _host;

        NetworkStream _netStream;
        SslStream _ssl;

        private readonly byte[] _packetTypeReadBuffer = new byte[2];
        private readonly Channel<(PacketType PacketType, object Packet, Type NetPacketType)> _writeChannel;


        public TcpSocket(IPEndPoint host, ILoggerFactory loggerFactory)
        {
            _host = host;
            _logger = loggerFactory.CreateLogger<TcpSocket>();
            _client = new TcpClient();

            _writeChannel =
                Channel.CreateUnbounded<(PacketType PacketType, object Packet, Type NetPacketType)>(
                    new UnboundedChannelOptions {SingleReader = true, SingleWriter = false});
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

            DateTime startWait = DateTime.Now;
            while (!_ssl.IsAuthenticated)
            {
                if (DateTime.Now - startWait > TimeSpan.FromSeconds(2))
                    throw new TimeoutException("Timed out waiting for ssl authentication");

                System.Threading.Thread.Sleep(10);
            }

            Handshake(username, password, tokens);

            Task.Factory.StartNew(Listen, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(SendDataSink, TaskCreationOptions.LongRunning);
        }

        public void Close()
        {
            _ssl = null;
            _netStream.Close();
            _client.Close();
        }

        private void Handshake(string username, string password, string[] tokens)
        {
            MumbleProto.Version version = new MumbleProto.Version
            {
                Release = "MumbleSharp",
                version = (1 << 16) | (2 << 8) | (5 & 0xFF),
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
            auth.CeltVersions = new [] {unchecked((int) 0x8000000b)};

            Send(PacketType.Authenticate, auth);
        }

        public void Send<T>(PacketType type, T packet)
        {
            _writeChannel.Writer.TryWrite((type, packet, packet.GetType()));
        }

        public void SendVoice(Span<byte> packet)
        {
            var arr = packet.ToArray();
            _writeChannel.Writer.TryWrite((PacketType.UDPTunnel, arr, arr.GetType()));
        }

        private void ReadPackage(PacketType type)
        {
            if (!_client.Connected)
                throw new InvalidOperationException("Not connected");

            object packet;
            _logger.LogTrace(type.ToString());

            switch (type)
            {
                case PacketType.Version:
                    packet = Serializer.DeserializeWithLengthPrefix<MumbleProto.Version>(_ssl, SerializationStyle);
                    break;
                case PacketType.CryptSetup:
                    packet = Serializer.DeserializeWithLengthPrefix<CryptSetup>(_ssl, SerializationStyle);
                    break;
                case PacketType.ChannelState:
                    packet = Serializer.DeserializeWithLengthPrefix<ChannelState>(_ssl, SerializationStyle);
                    break;
                case PacketType.UserState:
                    packet = Serializer.DeserializeWithLengthPrefix<UserState>(_ssl, SerializationStyle);
                    break;
                case PacketType.CodecVersion:
                    packet = Serializer.DeserializeWithLengthPrefix<CodecVersion>(_ssl, SerializationStyle);
                    break;
                case PacketType.ContextAction:
                    packet = Serializer.DeserializeWithLengthPrefix<ContextAction>(_ssl, SerializationStyle);
                    break;
                case PacketType.ContextActionModify:
                    packet = Serializer.DeserializeWithLengthPrefix<ContextActionModify>(_ssl, SerializationStyle);
                    break;
                case PacketType.PermissionQuery:
                    packet = Serializer.DeserializeWithLengthPrefix<PermissionQuery>(_ssl, SerializationStyle);
                    break;
                case PacketType.ServerSync:
                    packet = Serializer.DeserializeWithLengthPrefix<ServerSync>(_ssl, SerializationStyle);
                    break;
                case PacketType.ServerConfig:
                    packet = Serializer.DeserializeWithLengthPrefix<ServerConfig>(_ssl, SerializationStyle);
                    break;
                case PacketType.UDPTunnel:
                    using (var reader = new BinaryReader(_ssl,Encoding.Default, true))
                    {
                        var length = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                        packet = reader.ReadBytes(length);
                    }
                    break;
                case PacketType.Ping:
                    packet = Serializer.DeserializeWithLengthPrefix<Ping>(_ssl, SerializationStyle);
                    break;
                case PacketType.UserRemove:
                    packet = Serializer.DeserializeWithLengthPrefix<UserRemove>(_ssl, SerializationStyle);
                    break;
                case PacketType.ChannelRemove:
                    packet = Serializer.DeserializeWithLengthPrefix<ChannelRemove>(_ssl, SerializationStyle);
                    break;
                case PacketType.TextMessage:
                    packet = Serializer.DeserializeWithLengthPrefix<TextMessage>(_ssl, SerializationStyle);
                    break;
                case PacketType.UserList:
                    packet = Serializer.DeserializeWithLengthPrefix<UserList>(_ssl, SerializationStyle);
                    break;
                case PacketType.SuggestConfig:
                    packet = Serializer.DeserializeWithLengthPrefix<SuggestConfig>(_ssl, SerializationStyle);
                    break;
                default:
                    _logger.LogError($"Packet {type} not supported");
                    return;
            }

            OnPacketReceived(type, packet);
        }

        private async Task SendDataSink()
        {
            BinaryWriter writer = new BinaryWriter(_ssl);
            try
            {
                while (true)
                {
                    var data = await _writeChannel.Reader.ReadAsync();
                    writer.Write(IPAddress.HostToNetworkOrder((short)data.PacketType));
                    if (data.PacketType == PacketType.UDPTunnel)
                    {
                        var arr = (byte[]) data.Packet;
                        writer.Write(IPAddress.HostToNetworkOrder(arr.Length));
                        writer.Write(arr);

                        writer.Flush();
                    }
                    else
                    {
                        writer.Flush();

                        RuntimeTypeModel runtimeTypeModel = RuntimeTypeModel.Default;
                        runtimeTypeModel.SerializeWithLengthPrefix(_ssl, data.Packet, data.NetPacketType, SerializationStyle, 0);
                        //Serializer.SerializeWithLengthPrefix<T>(_ssl, packet, serializationStyle);
                    }

                    _ssl.Flush();
                    _netStream.Flush();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ssl writer exception");
            }
        }

        private async Task Listen()
        {
            while (_client.Connected)
            {
                await _ssl.ReadAsync(_packetTypeReadBuffer, 0, 2);
                var type = (PacketType)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(_packetTypeReadBuffer, 0));
                try
                {
                    ReadPackage(type);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private void OnPacketReceived(PacketType packetType, object packet)
        {
            var eventArgs = new PacketReceivedEventArgs(packetType, packet);
            PacketReceived?.Invoke(this, eventArgs);
        }
    }
}
