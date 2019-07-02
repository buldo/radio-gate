using MumbleProto;
using MumbleSharp.Packets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Timers;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using MumbleSharp.Services;

namespace MumbleSharp
{
    /// <summary>
    /// Handles the low level details of connecting to a mumble server. Once connection is established decoded packets are passed off to the MumbleProtocol for processing
    /// </summary>
    public class MumbleConnection
    {
        private readonly Timer _pingTimer = new Timer
        {
            AutoReset = true,
            Interval = 20000
        };

        private readonly ILogger<MumbleConnection> _logger;
        private readonly PingProcessor _pingProcessor = new PingProcessor();
        private readonly TcpSocket _tcp;
        private readonly UdpSocket _udp;

        private readonly Dictionary<PacketType, List<Action<object>>> _processors = new Dictionary<PacketType, List<Action<object>>>();
        private Action<byte[], int> _voicePacketProcessor;

        private UInt32 _sequenceIndex;
        private ConnectionStates _state;

        [PublicAPI]
        public ConnectionStates State
        {
            get => _state;
            private set
            {
                _state = value;
                if (_state == ConnectionStates.Connecting)
                {
                    _pingTimer.Enabled = true;
                }
                else if (_state == ConnectionStates.Disconnecting)
                {
                    _pingTimer.Enabled = false;
                }
            }
        }

        [PublicAPI]
        public IPEndPoint Host { get; }

        /// <summary>
        /// Creates a connection to the server
        /// </summary>
        public MumbleConnection(IPEndPoint host, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MumbleConnection>();
            _pingTimer.Elapsed += PingTimerOnElapsed;

            _tcp = new TcpSocket(Host, loggerFactory);
            _tcp.PacketReceived += TcpOnPacketReceived;

            _udp = new UdpSocket(Host, this);
            _udp.EncodedVoiceReceived += UdpOnEncodedVoiceReceived;

            Host = host;
            State = ConnectionStates.Disconnected;

            foreach(var val in Enum.GetValues(typeof(PacketType)))
            {
                _processors[(PacketType)val] = new List<Action<object>>();
            }
        }

        public void RegisterPacketProcessor(PacketProcessor processor)
        {
            _processors[processor.PacketType].Add(processor.Processor);
        }

        public void Connect(
            string username,
            string password,
            string[] tokens,
            string serverName,
            RemoteCertificateValidationCallback validateCertificate,
            LocalCertificateSelectionCallback selectCertificate)
        {
            if (State != ConnectionStates.Disconnected)
            {
                throw new InvalidOperationException($"Cannot start connecting MumbleConnection when connection state is {State}");
            }

            State = ConnectionStates.Connecting;

            _tcp.Connect(username, password, tokens, serverName, validateCertificate, selectCertificate);

            // UDP Connection is disabled while decryption is broken
            // See: https://github.com/martindevans/MumbleSharp/issues/4
            // UDP being disabled does not reduce functionality, it forces packets to be sent over TCP instead
            //_udp.Connect();

            State = ConnectionStates.Connected;
        }

        public void Close()
        {
            State = ConnectionStates.Disconnecting;

            //_udp.Close();
            _tcp.Close();

            State = ConnectionStates.Disconnected;
        }

        public void SendControl<T>(PacketType type, T packet)
        {
            _tcp.Send(type, packet);
        }

        public void SendEncodedVoice(Span<byte> packet)
        {
            //This is *totally wrong*
            //the packet contains raw encoded voice data, but we need to put it into the proper packet format
            //UPD: packet prepare before this method called. See basic protocol

            _logger.LogTrace($"{nameof(SendEncodedVoice)} invoked");

            if (packet != null)
            {
                if (packet.Length > 8191)
                {
                    _logger.LogError("Too big packet");
                }

                var packetBuilder = new UdpPacketBuilder();

                // Header
                int flags = 0;
                flags |= (int)MumbleUdpMessageType.VoiceOpus << 5;
                flags |= 0x00 & 0x1F; // TargetId
                packetBuilder.WriteByte((byte)(flags & 0xFF));

                // Packet Sequence Number
                _sequenceIndex += 2;
                packetBuilder.WriteVarLong(_sequenceIndex);

                // Length and Terminated bit
                int currentBlockSize = packet.Length;
                //if (terminated)
                //    currentBlockSize |= 1 << 13;
                packetBuilder.WriteVarLong(currentBlockSize);

                // Encoded data
                packetBuilder.Write(packet.ToArray());

                _tcp.SendVoice(packetBuilder.ToArray());
            }
        }

        public void RegisterVoicePacketProcessor(Action<byte[],int> processor)
        {
            _voicePacketProcessor = processor;
        }

        private void PingTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (_state != ConnectionStates.Connected)
            {
                return;
            }

            SendTcpPing();

            //if (_udp.IsConnected)
            //{
            //    _udp.SendPing();
            //}
        }

        private void TcpOnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            switch (e.PacketType)
            {
                case PacketType.CryptSetup:
                    //ProcessCryptState((CryptSetup) e.Packet);
                    SendTcpPing();
                    break;
                case PacketType.UDPTunnel:
                    _udp.ReceiveDecryptedUdp((byte[]) e.Packet);
                    break;
                case PacketType.Ping:
                    _pingProcessor.ReceivePing((Ping) e.Packet);
                    break;
            }

            foreach(var processor in _processors[e.PacketType])
            {
                processor(e.Packet);
            }
        }

        private void UdpOnEncodedVoiceReceived(object sender, VoiceReceivedEventArgs e)
        {
            _voicePacketProcessor?.Invoke(e.Data, e.Type);
        }

        private void SendTcpPing()
        {
            var ping = _pingProcessor.CreateTcpPing();
            SendControl(PacketType.Ping, ping);
        }

        public void SendUdpPing()
        {
            var buffer = _pingProcessor.CreateUdpPing();
            _udp.SendPing(buffer);
        }
    }
}