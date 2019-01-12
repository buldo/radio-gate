using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Gate.Opus;
using Gate.Opus.Api;

namespace MumbleSharp.Audio.Codecs.Opus
{
    public class OpusCodec
        : IVoiceCodec
    {
        private readonly int[] _permittedFrameSizes;
        private readonly OpusDecoder _decoder;
        private readonly OpusEncoder _encoder;
        private readonly OpusPacketApi _packetApi;
        
        public OpusCodec()
        {
            _decoder = OpusFactory.CreateDecoder(Constants.SAMPLE_RATE, Constants.CHANNELS);
            _decoder.IsForwardErrorCorrectionEnabled = true;
            _encoder = OpusFactory.CreateEncoder(Constants.SAMPLE_RATE, Constants.CHANNELS, Application.VoIP);
            _encoder.IsForwardErrorCorrectionEnabled = true;
            _packetApi = OpusFactory.GetPacketApi();

            float[] frameSizes = { 2.5f, 5, 10, 20, 40, 60 };

            _permittedFrameSizes = new int[frameSizes.Length];
            for (var i = 0; i < frameSizes.Length; i++)
            {
                _permittedFrameSizes[i] = (int)(Constants.SAMPLE_RATE / 1000f * frameSizes[i]);
            }
        }

        public byte[] Decode(byte[] encodedData)
        {
            if (encodedData == null)
            {
                _decoder.Decode(null, Constants.FRAME_SIZE);
                return null;
            }

            int samples = _packetApi.GetNbSamples(encodedData, encodedData.Length, Constants.SAMPLE_RATE);
            if (samples < 1)
                return null;

            var decodedShort = _decoder.Decode(encodedData, samples);
            return MemoryMarshal.Cast<short, byte>(decodedShort).ToArray();
        }

        public IEnumerable<int> PermittedEncodingFrameSizes => _permittedFrameSizes;

        public byte[] Encode(ArraySegment<byte> pcm)
        {
            var pcmInShort = MemoryMarshal.Cast<byte, short>(pcm.AsSpan());
            var encoded = _encoder.Encode(pcmInShort, pcmInShort.Length);
            return encoded.ToArray();
        }
    }
}
