using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Gate.Opus;
using Gate.Opus.Api;

namespace MumbleSharp.Voice.Codecs
{
    public class MumbleOpusEncoder: IDisposable
    {
        private readonly OpusEncoder _encoder;

        public MumbleOpusEncoder()
        {
            _encoder = OpusFactory.CreateEncoder(Constants.SAMPLE_RATE, Constants.CHANNELS, Application.VoIP);
            //_encoder.IsForwardErrorCorrectionEnabled = true;
            _encoder.IsVbrEnabled = false;
            _encoder.Bitrate = Constants.BITRATE;
        }

        public byte[] Encode(ReadOnlySpan<byte> pcm)
        {
            var pcmInShort = MemoryMarshal.Cast<byte, short>(pcm);
            var encoded = _encoder.Encode(pcmInShort, pcmInShort.Length);
            return encoded.ToArray();
        }

        public void Dispose()
        {
            _encoder.Dispose();
        }
    }
}
