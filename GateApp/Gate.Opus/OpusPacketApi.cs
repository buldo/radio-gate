using System;
using System.Collections.Generic;
using System.Text;
using Gate.Opus.Api;

namespace Gate.Opus
{
    public class OpusPacketApi
    {
        private readonly IOpusApi _api;

        internal OpusPacketApi(IOpusApi api)
        {
            _api = api;
        }

        public int GetNbSamples(byte[] packet, int len, int fs)
        {
            return _api.opus_packet_get_nb_samples(packet, len, fs);
        }
    }
}
