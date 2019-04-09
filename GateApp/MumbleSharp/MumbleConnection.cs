using MumbleProto;
using MumbleSharp.Audio;
using MumbleSharp.Audio.Codecs;
using MumbleSharp.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Timers;

namespace MumbleSharp
{
    /// <summary>
    /// Handles the low level details of connecting to a mumble server. Once connection is established decoded packets are passed off to the MumbleProtocol for processing
    /// </summary>
    public partial class MumbleConnection
    {
        private readonly Timer _pingTimer = new Timer()
        {
            AutoReset = true,
            Interval = 5000
        };

        private readonly Dictionary<PacketType, List<Action<object>>> _processors = new Dictionary<PacketType, List<Action<object>>>();
        private readonly List<Action<byte[], uint, long, SpeechCodec, SpeechTarget>> _voicePacketProcessors = new List<Action<byte[], uint, long, SpeechCodec, SpeechTarget>>();

        internal readonly PingProcessor _pingProcessor = new PingProcessor();

        internal readonly CryptState _cryptState = new CryptState();

        private ConnectionStates _state;

        private TcpSocket _tcp;
        private UdpSocket _udp;

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

        public IPEndPoint Host { get; }
        
        /// <summary>
        /// Creates a connection to the server using the given address and port.
        /// </summary>
        /// <param name="server">The server adress or IP.</param>
        /// <param name="port">The port the server listens to.</param>
        /// <param name="protocol">An object which will handle messages from the server</param>
        public MumbleConnection(string server, int port)
            : this(new IPEndPoint(
                Dns.GetHostAddresses(server).First(a => a.AddressFamily == AddressFamily.InterNetwork), port))
        {
        }

        /// <summary>
        /// Creates a connection to the server
        /// </summary>
        /// <param name="host"></param>
        /// <param name="protocol"></param>
        public MumbleConnection(IPEndPoint host)
        {
            _pingTimer.Elapsed += PingTimerOnElapsed;
            Host = host;
            State = ConnectionStates.Disconnected;

            foreach(var val in Enum.GetValues(typeof(PacketType)))
            {
                _processors[(PacketType)val] = new List<Action<object>>();
            }
        }
        

        public void RegisterPacketProcessor(PacketType packetType, Action<object> processor)
        {
            _processors[packetType].Add(processor);
        }

        public void RegisterVoicePacketProcessor(Action<byte[], uint, long, SpeechCodec, SpeechTarget> processor)
        {

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
                throw new InvalidOperationException(
                    string.Format("Cannot start connecting MumbleConnection when connection state is {0}", State));

            State = ConnectionStates.Connecting;

            _tcp = new TcpSocket(Host, this);
            _tcp.PacketReceived += TcpOnPacketReceived;
            _tcp.Connect(username, password, tokens, serverName, validateCertificate, selectCertificate);

            // UDP Connection is disabled while decryption is broken
            // See: https://github.com/martindevans/MumbleSharp/issues/4
            // UDP being disabled does not reduce functionality, it forces packets to be sent over TCP instead
            _udp = new UdpSocket(Host, this);
            _udp.EncodedVoiceReceived += UdpOnEncodedVoiceReceived;
            //_udp.Connect();

            State = ConnectionStates.Connected;
        }

        public void Close()
        {
            State = ConnectionStates.Disconnecting;

            _udp.Close();
            _tcp.Close();

            State = ConnectionStates.Disconnected;
        }

        public void Process()
        {
            _tcp.Process();
            _udp.Process();
        }

        public void SendControl<T>(PacketType type, T packet)
        {
            _tcp.Send<T>(type, packet);
        }

        public void SendVoice(ArraySegment<byte> packet)
        {
            //This is *totally wrong*
            //the packet contains raw encoded voice data, but we need to put it into the proper packet format
            //UPD: packet prepare before this method called. See basic protocol

            _tcp.SendVoice(PacketType.UDPTunnel, packet);
        }

        private void PackVoicePacket(ArraySegment<byte> packet)
        {
        }

        internal void ProcessCryptState(CryptSetup cryptSetup)
        {
            if (cryptSetup.ShouldSerializeKey() && cryptSetup.ShouldSerializeClientNonce() &&
                cryptSetup.ShouldSerializeServerNonce()) // Full key setup
            {
                _cryptState.SetKeys(cryptSetup.Key, cryptSetup.ClientNonce, cryptSetup.ServerNonce);
            }
            else if (cryptSetup.ServerNonce != null) // Server syncing its nonce to us.
            {
                _cryptState.ServerNonce = cryptSetup.ServerNonce;
            }
            else // Server wants our nonce.
            {
                SendControl<CryptSetup>(PacketType.CryptSetup, new CryptSetup {ClientNonce = _cryptState.ClientNonce});
            }
        }

        private void PingTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (_state != ConnectionStates.Connected)
            {
                return;
            }

            _tcp.SendPing();

            if (_udp.IsConnected)
            {
                _udp.SendPing();
            }
        }

        private void TcpOnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            switch (e.PacketType)
            {
                case PacketType.CryptSetup:
                    ProcessCryptState((CryptSetup) e.Packet);
                    _tcp.SendPing();
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

        private void UdpOnEncodedVoiceReceived(object sender, EncodedVoiceReceivedEventArgs e)
        {
            foreach(var processor in _voicePacketProcessors)
            {
                processor(e.Data, e.Session, e.Sequence, e.Codec, e.Target);
            }
        }
    }
}