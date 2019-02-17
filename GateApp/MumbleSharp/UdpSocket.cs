using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using MumbleSharp.Audio;
using MumbleSharp.Audio.Codecs;

namespace MumbleSharp
{
    internal class UdpSocket
    {
        readonly UdpClient _client;
        readonly IPEndPoint _host;
        readonly MumbleConnection _connection;

        public UdpSocket(IPEndPoint host, MumbleConnection connection)
        {
            _host = host;
            _connection = connection;
            _client = new UdpClient();
        }

        public event EventHandler<PingReceivedEventArgs> PingReceived;
        public event EventHandler<EncodedVoiceReceivedEventArgs> EncodedVoiceReceived;

        public bool IsConnected { get; private set; }

        public void Connect()
        {
            _client.Connect(_host);
            IsConnected = true;
        }

        public void Close()
        {
            IsConnected = false;
            _client.Close();
        }

        public void SendPing()
        {
            var buffer = _connection._pingProcessor.CreateUdpPing();
            _client.Send(buffer, buffer.Length);
        }

        public void Process()
        {
            if (_client.Client == null)
                return;
            if (_client.Available == 0)
                return;

            IPEndPoint sender = _host;
            byte[] data = _client.Receive(ref sender);

            ReceivedEncryptedUdp(data);
        }

        private void ReceivedEncryptedUdp(byte[] packet)
        {
            byte[] plaintext = _connection._cryptState.Decrypt(packet, packet.Length);

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
                OnPingReceived(packet);
            else
                UnpackVoicePacket(packet, type);
        }

        private void UnpackVoicePacket(byte[] packet, int type)
        {
            var vType = (SpeechCodec)type;
            var target = (SpeechTarget)(packet[0] & 0x1F);

            using (var reader = new UdpPacketReader(new MemoryStream(packet, 1, packet.Length - 1)))
            {
                UInt32 session = (uint)reader.ReadVarInt64();
                Int64 sequence = reader.ReadVarInt64();

                int size = (int)reader.ReadVarInt64();
                size &= 0x1fff;

                if (size == 0)
                    return;

                byte[] data = reader.ReadBytes(size);
                if (data == null)
                    return;

                OnEncodedVoiceReceived(data, session, sequence, target, vType);
            }
        }

        private void OnEncodedVoiceReceived(byte[] data, uint session, long sequence, SpeechTarget target, SpeechCodec codec)
        {
            var eventArgs = new EncodedVoiceReceivedEventArgs(data, session, sequence, target, codec);
            EncodedVoiceReceived?.Invoke(this, eventArgs);
        }

        private void OnPingReceived(byte[] packet)
        {
            PingReceived?.Invoke(this, new PingReceivedEventArgs(packet));
        }
    }
}