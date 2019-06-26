using System;

namespace MumbleSharp
{
    public class VoiceReceivedEventArgs : EventArgs
    {
        public VoiceReceivedEventArgs(
            byte[] data,
            int type
            )
        {
            Data = data;
            Type = type;
        }

        public byte[] Data { get; }

        public int Type { get; }
    }
}
