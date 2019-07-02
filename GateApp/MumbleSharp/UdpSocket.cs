using System;
using System.Net;
using System.Net.Sockets;

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
        public event EventHandler<VoiceReceivedEventArgs> EncodedVoiceReceived;

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

        public void SendPing(byte[] ping)
        {
            _client.Send(ping, ping.Length);
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
            //byte[] plaintext = _connection._cryptState.Decrypt(packet, packet.Length);

            //if (plaintext == null)
            //{
            //    Console.WriteLine("Decryption failed");
            //    return;
            //}

            // ReceiveDecryptedUdp(plaintext);
        }

        internal void ReceiveDecryptedUdp(byte[] packet)
        {
            var type = packet[0] >> 5 & 0x7;

            if (type == 1)
                OnPingReceived(packet);
            else
                OnVoiceReceived(packet, type);
        }

        private void OnVoiceReceived(byte[] data, int type)
        {
            var eventArgs = new VoiceReceivedEventArgs(data, type);
            EncodedVoiceReceived?.Invoke(this, eventArgs);
        }

        private void OnPingReceived(byte[] packet)
        {
            PingReceived?.Invoke(this, new PingReceivedEventArgs(packet));
        }
    }
}