using System;
using NAudio.Wave;
using PulseAudioNet.Api;

namespace NAudio.Pulse
{
    public static class FormatConverter
    {
        public static SampleSpec Convert(WaveFormat waveFormat)
        {
            SampleFormat format;
            switch (waveFormat.Encoding)
            {
                case WaveFormatEncoding.Unknown:
                    format = SampleFormat.Invalid;
                    break;
                case WaveFormatEncoding.Pcm:
                {
                    switch (waveFormat.BitsPerSample)
                    {
                        case 8:
                            format = SampleFormat.U8;
                            break;
                        case 16:
                            format = SampleFormat.S16Le;
                            break;
                        case 24:
                            format = SampleFormat.S24Le;
                            break;
                        case 32:
                            format = SampleFormat.S32Le;
                            break;
                        default:
                            throw new NotImplementedException($"Conversion not implemented for PCM {waveFormat.BitsPerSample} bits");
                    }
                }
                break;
                case WaveFormatEncoding.MuLaw:
                    format = SampleFormat.ULaw;
                    break;
                case WaveFormatEncoding.ALaw:
                    format = SampleFormat.ALaw;
                    break;
                default:
                    throw new NotImplementedException($"Conversion not implemented for {waveFormat.Encoding}");
            }

            return new SampleSpec
            {
                Rate = (uint) waveFormat.SampleRate,
                Channels = (byte) waveFormat.Channels,
                Format = format
            };
        }
    }
}