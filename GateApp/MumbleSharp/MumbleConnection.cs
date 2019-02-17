using MumbleProto;
using MumbleSharp.Audio;
using MumbleSharp.Audio.Codecs;
using MumbleSharp.Packets;
using System;
using System.IO;
using System.Linq;
using System.Net;
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

        internal readonly PingProcessor _pingProcessor = new PingProcessor();

        private readonly CryptState _cryptState = new CryptState();

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

        public IMumbleProtocol Protocol { get; private set; }

        public IPEndPoint Host { get; }


        /// <summary>
        /// Creates a connection to the server using the given address and port.
        /// </summary>
        /// <param name="server">The server adress or IP.</param>
        /// <param name="port">The port the server listens to.</param>
        /// <param name="protocol">An object which will handle messages from the server</param>
        public MumbleConnection(string server, int port, IMumbleProtocol protocol)
            : this(
                new IPEndPoint(Dns.GetHostAddresses(server).First(a => a.AddressFamily == AddressFamily.InterNetwork),
                    port), protocol)
        {
        }

        /// <summary>
        /// Creates a connection to the server
        /// </summary>
        /// <param name="host"></param>
        /// <param name="protocol"></param>
        public MumbleConnection(IPEndPoint host, IMumbleProtocol protocol)
        {
            _pingTimer.Elapsed += PingTimerOnElapsed;
            Host = host;
            State = ConnectionStates.Disconnected;
            Protocol = protocol;
        }

        public void Connect(string username, string password, string[] tokens, string serverName)
        {
            if (State != ConnectionStates.Disconnected)
                throw new InvalidOperationException(
                    string.Format("Cannot start connecting MumbleConnection when connection state is {0}", State));

            State = ConnectionStates.Connecting;
            Protocol.Initialise(this);

            _tcp = new TcpSocket(Host, Protocol, this);
            _tcp.Connect(username, password, tokens, serverName);

            // UDP Connection is disabled while decryption is broken
            // See: https://github.com/martindevans/MumbleSharp/issues/4
            // UDP being disabled does not reduce functionality, it forces packets to be sent over TCP instead
            _udp = new UdpSocket(Host, Protocol, this);
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

        internal void ReceivedEncryptedUdp(byte[] packet)
        {
            byte[] plaintext = _cryptState.Decrypt(packet, packet.Length);

            if (plaintext == null)
            {
                Console.WriteLine("Decryption failed");
                return;
            }

            ReceiveDecryptedUdp(plaintext);
        }

        internal void ReceiveDecryptedUdp(byte[] packet)
        {
            var type = packet[0] >> 5 & 0x7;

            if (type == 1)
                Protocol.UdpPing(packet);
            else
                UnpackVoicePacket(packet, type);
        }

        private void PackVoicePacket(ArraySegment<byte> packet)
        {
        }

        private void UnpackVoicePacket(byte[] packet, int type)
        {
            var vType = (SpeechCodecs) type;
            var target = (SpeechTarget) (packet[0] & 0x1F);

            using (var reader = new UdpPacketReader(new MemoryStream(packet, 1, packet.Length - 1)))
            {
                UInt32 session = (uint) reader.ReadVarInt64();
                Int64 sequence = reader.ReadVarInt64();

                //Null codec means the user was not found. This can happen if a user leaves while voice packets are still in flight
                IVoiceCodec codec = Protocol.GetCodec(session, vType);
                if (codec == null)
                    return;

                if (vType == SpeechCodecs.Opus)
                {
                    int size = (int) reader.ReadVarInt64();
                    size &= 0x1fff;

                    if (size == 0)
                        return;

                    byte[] data = reader.ReadBytes(size);
                    if (data == null)
                        return;

                    Protocol.EncodedVoice(data, session, sequence, codec, target);
                }
                else
                {
                    throw new NotImplementedException("Codec is not opus");

                    //byte header;
                    //do
                    //{
                    //    header = reader.ReadByte();
                    //    int length = header & 0x7F;
                    //    if (length > 0)
                    //    {
                    //        byte[] data = reader.ReadBytes(length);
                    //        if (data == null)
                    //            break;

                    //        //TODO: Put *encoded* packets into a queue, then decode the head of the queue
                    //        //TODO: This allows packets to come into late and be inserted into the correct place in the queue (if they arrive before decoding handles a later packet)
                    //        byte[] decodedPcmData = codec.Decode(data);
                    //        if (decodedPcmData != null)
                    //            Protocol.Voice(decodedPcmData, session, sequence);
                    //    }

                    //} while ((header & 0x80) > 0);
                }
            }
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
    }
}