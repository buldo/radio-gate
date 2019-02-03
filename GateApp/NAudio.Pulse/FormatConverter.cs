using System;
using NAudio.Wave;
using PulseAudioNet.Api;

namespace NAudio.Pulse
{
    public static class FormatConverter
    {
        public static SampleFormat Convert(WaveFormatEncoding from)
        {
            switch (from)
            {
                case WaveFormatEncoding.Unknown:
                    return SampleFormat.Invalid;
                case WaveFormatEncoding.Pcm:
                    return SampleFormat.U8;
                case WaveFormatEncoding.MuLaw:
                    return SampleFormat.ULaw;
                case WaveFormatEncoding.ALaw:
                    return SampleFormat.ALaw;

                default:
                    throw new NotImplementedException($"Conversion not implemented for {from}");
            }
        }
    }
}