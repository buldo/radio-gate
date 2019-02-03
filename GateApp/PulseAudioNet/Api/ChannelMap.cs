namespace PulseAudioNet.Api
{
    public unsafe struct ChannelMap
    {
        public byte Channels;
        public fixed int Map[32];
    }
}