using System;

namespace MumbleSharp
{
    public class PingReceivedEventArgs : EventArgs
    {
        public PingReceivedEventArgs(byte[] ping)
        {
            Ping = ping;
        }

        public byte[] Ping { get; }
    }
}
