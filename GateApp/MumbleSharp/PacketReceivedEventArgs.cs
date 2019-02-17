using System;
using MumbleSharp.Packets;

namespace MumbleSharp
{
    public class PacketReceivedEventArgs : EventArgs
    {
        public PacketReceivedEventArgs(PacketType packetType, object packet)
        {
            PacketType = packetType;
            Packet = packet;
        }

        public PacketType PacketType { get; }

        public object Packet { get; }
    }
}
