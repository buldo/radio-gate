using System;
using System.Collections.Generic;
using MumbleProto;
using MumbleSharp.Packets;

namespace MumbleSharp.Services
{
    public class ServerSyncStateService : IService
    {
        /// <summary>
        /// If true, this indicates that the connection was setup and the server accept this client
        /// </summary>
        public bool ReceivedServerSync { get; private set; }

        public uint Session { get; private set; }

        public event EventHandler<EventArgs> SyncReceived;

        IEnumerable<PacketProcessor> IService.GetProcessors()
        {
            return new[]
            {
                new PacketProcessor(PacketType.ServerSync, ProcessServerSyncPacket),
            };
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
