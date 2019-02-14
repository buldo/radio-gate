using System;
using System.Collections.Generic;
using System.Text;
using AdvancedDLSupport;
using Gate.Opus.Api;

namespace Gate.Opus
{
    public static class OpusFactory
    {
        private static readonly object CreateLock = new object();
        private static IOpusApi _api;
        private static OpusPacketApi _packetApi;

        /// <summary>
        /// Allocates and initializes an encoder state.
        /// </summary>
        /// <param name="samplingRate">
        /// Sampling rate of input signal (Hz)
        /// This must be one of 8000, 12000, 16000, 24000, or 48000.
        /// </param>
        /// <param name="channels">Number of channels (1 or 2) in input signal</param>
        /// <param name="application">
        /// Coding mode
        /// </param>
        /// <param name="error">Error code</param>
        /// <returns>Encoder</returns>
        /// <remarks>
        /// There are three coding modes:
        ///
        /// OPUS_APPLICATION_VOIP gives best quality at a given bitrate for voice
        ///    signals.It enhances the  input signal by high-pass filtering and
        ///    emphasizing formants and harmonics.Optionally it includes in-band
        ///    forward error correction to protect against packet loss.Use this
        ///    mode for typical VoIP applications.Because of the enhancement,
        ///    even at high bitrates the output may sound different from the input.
        ///
        /// OPUS_APPLICATION_AUDIO gives best quality at a given bitrate for most
        ///    non-voice signals like music. Use this mode for music and mixed
        ///    (music/voice) content, broadcast, and applications requiring less
        ///    than 15 ms of coding delay.
        ///
        /// OPUS_APPLICATION_RESTRICTED_LOWDELAY configures low-delay mode that
        ///    disables the speech-optimized mode in exchange for slightly reduced delay.
        ///    This mode can only be set on an newly initialized or freshly reset encoder
        ///    because it changes the codec delay.
        ///
        /// This is useful when the caller knows that the speech-optimized modes will not be needed (use with caution).
        /// </remarks>
        /// <remarks>
        /// Regardless of the sampling rate and number channels selected, the Opus encoder
        /// can switch to a lower audio bandwidth or number of channels if the bitrate
        /// selected is too low. This also means that it is safe to always use 48 kHz stereo input
        /// and let the encoder optimize the encoding.
        /// </remarks>
        public static OpusEncoder CreateEncoder(int samplingRate, int channels, Application application)
        {
            if (samplingRate != 8000 &&
                samplingRate != 12000 &&
                samplingRate != 16000 &&
                samplingRate != 24000 &&
                samplingRate != 48000)
                throw new ArgumentOutOfRangeException(nameof(samplingRate));
            if (channels != 1 && channels != 2)
                throw new ArgumentOutOfRangeException(nameof(channels));

            var api = GetApi();
            return new OpusEncoder(api, samplingRate, channels, application);
        }

        public static OpusDecoder CreateDecoder(int samplingRate, int channels)
        {
            if (samplingRate != 8000 &&
                samplingRate != 12000 &&
                samplingRate != 16000 &&
                samplingRate != 24000 &&
                samplingRate != 48000)
                throw new ArgumentOutOfRangeException(nameof(samplingRate));
            if (channels != 1 && channels != 2)
                throw new ArgumentOutOfRangeException(nameof(channels));

            var api = GetApi();
            return new OpusDecoder(api, samplingRate, channels);
        }

        public static OpusPacketApi GetPacketApi()
        {
            if (_packetApi == null)
            {
                _packetApi = new OpusPacketApi(GetApi());
            }

            return _packetApi;
        }

        private static IOpusApi GetApi()
        {
            if (_api == null)
            {
                lock (CreateLock)
                {
                    if (_api == null)
                    {
                        string libraryPath;
                        if (Environment.OSVersion.Platform == PlatformID.Unix)
                        {
                            libraryPath = "libopus.so.0";
                        }
                        else
                        {
                            libraryPath = "opus";
                        }
                        _api = NativeLibraryBuilder.Default.ActivateInterface<IOpusApi>(libraryPath);
                    }
                }
            }

            return _api;
        }
    }
}
