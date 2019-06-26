using System;
using MumbleSharp.Packets;

namespace MumbleSharp.Services
{
    public class PacketProcessor
    {
        public PacketProcessor(PacketType packetType, Action<object> processor)
        {
            PacketType = packetType;
            Processor = processor;
        }

        public PacketType PacketType { get; }
        public Action<object> Processor { get; }
    }
}