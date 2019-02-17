using System.Net;
using System.Net.Sockets;

namespace MumbleSharp
{
    internal class UdpSocket
    {
        readonly UdpClient _client;
        readonly IPEndPoint _host;
        readonly IMumbleProtocol _protocol;
        readonly MumbleConnection _connection;

        public bool IsConnected { get; private set; }

        public UdpSocket(IPEndPoint host, IMumbleProtocol protocol, MumbleConnection connection)
        {
            _host = host;
            _protocol = protocol;
            _connection = connection;
            _client = new UdpClient();
        }

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

            _connection.ReceivedEncryptedUdp(data);
        }
    }
}