using System;
using Microsoft.Extensions.Logging;
using MumbleProto;
using MumbleSharp.Packets;

namespace MumbleSharp.Services
{
    public class ServerSyncStateService
    {
        private readonly ILogger _logger;

        public ServerSyncStateService(MumbleConnection connection, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ServerSyncStateService>();
            connection.RegisterPacketProcessor(new PacketProcessor(PacketType.ServerSync, ProcessServerSyncPacket));
        }

        /// <summary>
        /// If true, this indicates that the connection was setup and the server accept this client
        /// </summary>
        public bool ReceivedServerSync { get; private set; }

        public uint Session { get; private set; }

        public event EventHandler<EventArgs> SyncReceived;

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

            _logger.LogInformation($"ServiceSync. Session: {serverSync.Session}{Environment.NewLine}{serverSync.WelcomeText}");
            ReceivedServerSync = true;
            Session = serverSync.Session;
            SyncReceived?.Invoke(this, EventArgs.Empty);
        }
    }
}
