using MumbleSharp.Audio.Codecs;
using System;

namespace MumbleSharp.Audio
{
    public class CodecSet
    {
        private readonly Lazy<OpusCodec> _opus = new Lazy<OpusCodec>();

        protected internal IVoiceCodec GetCodec(SpeechCodec codec)
        {
            switch (codec)
            {
                case SpeechCodec.Opus:
                    return _opus.Value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(codec), "Unsupported coded");
            }
        }
    }
}
