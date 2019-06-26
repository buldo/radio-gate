using System;
using MumbleProto;
using MumbleSharp.Packets;

namespace MumbleSharp.Services
{
    public class ServerSyncStateService
    {
        /// <summary>
        /// If true, this indicates that the connection was setup and the server accept this client
        /// </summary>
        public bool ReceivedServerSync { get; private set; }

        public uint Session { get; private set; }

        public event EventHandler<EventArgs> SyncReceived;

        public ServerSyncStateService(MumbleConnection connection)
        {
            connection.RegisterPacketProcessor(new PacketProcessor(PacketType.ServerSync, ProcessServerSyncPacket));
        }

        /// <summary>
        /// Initial connection to the server
        /// </summary>
        private void ProcessServerSyncPacket(object packet)
        {
            var serverSync = (ServerSync)packet;
            if (ReceivedServerSync)
            {
                throw new InvalidOperationException("Second ServerSync Received");
            }

            ReceivedServerSync = true;
            Session = serverSync.Session;
            SyncReceived?.Invoke(this, EventArgs.Empty);

            // _encodingBuffer = new AudioEncodingBuffer();
            //_encodingThread.Start();


        }
    }
}
