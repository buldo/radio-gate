using System;
using MumbleSharp.Audio;
using MumbleSharp.Audio.Codecs;

namespace MumbleSharp
{
    public class EncodedVoiceReceivedEventArgs : EventArgs
    {
        public EncodedVoiceReceivedEventArgs(
            byte[] data,
            uint session,
            long sequence,
            SpeechTarget target,
            SpeechCodec codec)
        {
            Data = data;
            Session = session;
            Sequence = sequence;
            Target = target;
            Codec = codec;
        }

        public byte[] Data { get; }

        public uint Session { get; }

        public long Sequence { get; }

        public SpeechTarget Target { get; }

        public SpeechCodec Codec { get; }
    }
}
