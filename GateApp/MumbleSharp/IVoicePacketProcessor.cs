using System;
using System.Collections.Generic;
using System.Text;

namespace MumbleSharp
{
    public interface IVoicePacketProcessor
    {
        void ProcessPackage(byte[] packet, int type);
    }
}
