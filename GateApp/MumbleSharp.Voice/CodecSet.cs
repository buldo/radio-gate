using System;
using MumbleSharp.Voice.Codecs;

namespace MumbleSharp.Voice
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
                    throw new ArgumentOutOfRangeException(nameof(codec), "Unsupported codec");
            }
        }
    }
}
