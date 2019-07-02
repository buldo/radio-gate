using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Gate.Opus;

namespace MumbleSharp.Voice.Codecs
{
    public class MumbleOpusDecoder
    {
        private readonly OpusDecoder _decoder;

        private readonly OpusPacketApi _packetApi;

        public MumbleOpusDecoder()
        {
            _decoder = OpusFactory.CreateDecoder(Constants.SAMPLE_RATE, Constants.CHANNELS);
            //_decoder.IsForwardErrorCorrectionEnabled = true;

            _packetApi = OpusFactory.GetPacketApi();
        }

        public byte[] Decode(byte[] encodedData)
        {
            if (encodedData == null)
            {
                _decoder.Decode(null, MumbleOpusHelper.PermittedEncodingFrameSizes[3]);
                return null;
            }

            int samples = _packetApi.GetNbSamples(encodedData, encodedData.Length, Constants.SAMPLE_RATE);
            if (samples < 1)
                return null;

            var decodedShort = _decoder.Decode(encodedData, samples);
            return MemoryMarshal.Cast<short, byte>(decodedShort).ToArray();
        }

        public void Dispose()
        {
            _decoder.Dispose();
        }
    }
}
