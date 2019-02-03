using System;

namespace PulseAudioNet.Api
{
    public struct SampleSpec
    {
        public SampleFormat Format;
        public uint Rate;
        public byte Channels;
    }
}