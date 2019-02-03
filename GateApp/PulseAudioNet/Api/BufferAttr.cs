using System;

namespace PulseAudioNet.Api
{
    public struct BufferAttr
    {
        public uint MaxLength;
        public uint TLength;
        public uint PreBuf;
        public uint MinReq;
        public uint FragSize;
    }
}