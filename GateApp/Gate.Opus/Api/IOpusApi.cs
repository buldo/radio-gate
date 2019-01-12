using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DLSupportDynamicAssembly")]
namespace Gate.Opus.Api
{
    /// <summary>
    /// The Opus codec is designed for interactive speech and audio transmission over the Internet.
    /// It is designed by the IETF Codec Working Group and incorporates technology from
    /// Skype's SILK codec and Xiph.Org's CELT codec.
    ///
    /// The Opus codec is designed to handle a wide range of interactive audio applications,
    /// including Voice over IP, videoconferencing, in-game chat, and even remote live music
    /// performances.It can scale from low bit-rate narrowband speech to very high quality
    /// stereo music.Its main features are:
    ///
    /// -Sampling rates from 8 to 48 kHz
    /// -Bit-rates from 6 kb/s to 510 kb/s
    /// -Support for both constant bit-rate (CBR) and variable bit-rate (VBR)
    /// -Audio bandwidth from narrowband to full-band
    /// -Support for speech and music
    /// -Support for mono and stereo
    /// -Support for multichannel (up to 255 channels)
    /// -Frame sizes from 2.5 ms to 60 ms
    /// -Good loss robustness and packet loss concealment(PLC)
    /// -Floating point and fixed-point implementation
    /// </summary>
    internal interface IOpusApi
    {
        #region Encoder

        /// Opus Encoder
        ///
        /// Since Opus is a stateful codec, the encoding process starts with creating an encoder
        /// state.This can be done with:
        /// 
        /// int error;
        /// OpusEncoder * enc;
        /// enc = opus_encoder_create(Fs, channels, application, error);
        /// 
        /// From this point, enc can be used for encoding an audio stream. An encoder state
        /// must not be used for more than one stream at the same time. Similarly, the encoder
        /// state must not be re-initialized for each frame.
        /// 
        /// While opus_encoder_create() allocates memory for the state, it's also possible
        /// to initialize pre-allocated memory:
        /// 
        /// int size;
        /// int error;
        /// OpusEncoder* enc;
        /// size = opus_encoder_get_size(channels);
        /// enc = malloc(size);
        /// error = opus_encoder_init(enc, Fs, channels, application);
        /// 
        /// where opus_encoder_get_size() returns the required size for the encoder state.Note that
        /// future versions of this code may change the size, so no assuptions should be made about it.
        /// 
        /// The encoder state is always continuous in memory and only a shallow copy is sufficient
        /// to copy it (e.g.memcpy())
        /// 
        /// It is possible to change some of the encoder's settings using the opus_encoder_ctl()
        /// interface. All these settings already default to the recommended value, so they should
        /// only be changed when necessary.The most common settings one may want to change are:
        /// 
        /// opus_encoder_ctl(enc, OPUS_SET_BITRATE(bitrate));
        /// opus_encoder_ctl(enc, OPUS_SET_COMPLEXITY(complexity));
        /// opus_encoder_ctl(enc, OPUS_SET_SIGNAL(signal_type));
        /// 
        /// where
        /// - bitrate is in bits per second(b/s)
        /// - complexity is a value from 1 to 10, where 1 is the lowest complexity and 10 is the highest
        /// - signal_type is either OPUS_AUTO(default), OPUS_SIGNAL_VOICE, or OPUS_SIGNAL_MUSIC
        /// 
        /// See opus_encoderctls and opus_genericctls for a complete list of parameters that can be set or queried.Most parameters can be set or changed at any time during a stream.
        ///  
        /// To encode a frame, opus_encode() or opus_encode_float() must be called with exactly one frame(2.5, 5, 10, 20, 40 or 60 ms) of audio data:
        /// len = opus_encode(enc, audio_frame, frame_size, packet, max_packet);
        /// 
        /// where
        /// - audio_frame is the audio data in opus_int16(or float for opus_encode_float())
        /// - frame_size is the duration of the frame in samples(per channel)
        /// - packet is the byte array to which the compressed data is written
        /// - max_packet is the maximum number of bytes that can be written in the packet(4000 bytes is recommended).
        /// Do not use max_packet to control VBR target bitrate, instead use the #OPUS_SET_BITRATE CTL.
        /// 
        /// opus_encode() and opus_encode_float() return the number of bytes actually written to the packet.
        /// The return value<b> can be negative</b>, which indicates that an error has occurred.If the return value
        /// is 2 bytes or less, then the packet does not need to be transmitted (DTX).
        /// 
        /// Once the encoder state if no longer needed, it can be destroyed with
        ///
        /// opus_encoder_destroy(enc);
        /// 
        /// If the encoder was created with opus_encoder_init() rather than opus_encoder_create(),
        /// then no action is required aside from potentially freeing the memory that was manually
        /// allocated for it(calling free(enc) for the example above)
        /// <summary>
        /// Gets the size of an OpusEncoder structure.
        /// </summary>
        /// <param name="channels">
        /// Number of channels.
        /// This must be 1 or 2.
        /// </param>
        /// <returns>The size in bytes.</returns>
        int opus_encoder_get_size(int channels);

        /// <summary>
        /// Allocates and initializes an encoder state.
        /// </summary>
        /// <param name="fs">
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
        IntPtr opus_encoder_create(int fs, int channels, Application application, out int error);

        /// <summary>
        /// Initializes a previously allocated encoder state
        /// The memory pointed to by st must be at least the size returned by opus_encoder_get_size().
        /// This is intended for applications which use their own allocator instead of malloc.
        /// To reset a previously initialized state, use the #OPUS_RESET_STATE CTL.
        /// </summary>
        /// <param name="st">Encoder state</param>
        /// <param name="fs">
        /// Sampling rate of input signal (Hz)
        /// This must be one of 8000, 12000, 16000, 24000, or 48000.
        /// </param>
        /// <param name="channels">Number of channels (1 or 2) in input signal</param>
        /// <param name="application">Coding mode (OPUS_APPLICATION_VOIP/OPUS_APPLICATION_AUDIO/OPUS_APPLICATION_RESTRICTED_LOWDELAY)</param>
        /// <returns>#OPUS_OK Success or opus_errorcodes</returns>
        int opus_encoder_init(IntPtr st, int fs, int channels, Application application);

        /// <summary>
        /// Encodes an Opus frame.
        /// </summary>
        /// <param name="st">Encoder state</param>
        /// <param name="pcm">Input signal (interleaved if 2 channels). length is frame_size*channels*sizeof(opus_int16)</param>
        /// <param name="frameSize">
        /// Number of samples per channel in the input signal.
        /// This must be an Opus frame size for the encoder's sampling rate.
        /// For example, at 48 kHz the permitted values are 120, 240, 480, 960, 1920, and 2880.
        /// Passing in a duration of less than 10 ms (480 samples at 48 kHz) will prevent the encoder from using the LPC or hybrid modes.
        /// </param>
        /// <param name="data">
        /// Output payload.
        /// This must contain storage for at least max_data_bytes.
        /// </param>
        /// <param name="maxDataBytes">
        /// Size of the allocated memory for the output payload.
        /// This may be used to impose an upper limit on the instant bitrate, but should not be used as the only bitrate control.
        /// Use #OPUS_SET_BITRATE to control the bitrate.
        /// </param>
        /// <returns>
        /// The length of the encoded packet (in bytes) on success or a negative error code (see @ref opus_errorcodes) on failure.
        /// </returns>
        int opus_encode(IntPtr st, short[] pcm, int frameSize, byte[] data, int maxDataBytes);

        /// <summary>
        /// Encodes an Opus frame from floating point input.
        /// </summary>
        /// <param name="st">Encoder state</param>
        /// <param name="pcm">
        /// Input in float format (interleaved if 2 channels), with a normal range of +/-1.0.
        /// Samples with a range beyond +/-1.0 are supported but will
        /// be clipped by decoders using the integer API and should
        /// only be used if it is known that the far end supports
        /// extended dynamic range.
        /// length is frame_size*channels*sizeof(float)
        /// </param>
        /// <param name="frameSize">
        /// Number of samples per channel in the input signal.
        /// This must be an Opus frame size for the encoder's sampling rate.
        /// For example, at 48 kHz the permitted values are 120, 240, 480, 960, 1920, and 2880.
        /// Passing in a duration of less than 10 ms (480 samples at 48 kHz) will prevent the encoder from using the LPC or hybrid modes.
        /// </param>
        /// <param name="data">
        /// Output payload.
        /// This must contain storage for at least max_data_bytes.
        /// </param>
        /// <param name="maxDataBytes">
        /// Size of the allocated memory for the output payload.
        /// This may be used to impose an upper limit on the instant bitrate,
        /// but should not be used as the only bitrate control.
        /// Use #OPUS_SET_BITRATE to control the bitrate.
        /// </param>
        /// <returns>
        /// The length of the encoded packet (in bytes) on success or a negative error code on failure.
        /// </returns>
        int opus_encode_float(IntPtr st, float[] pcm, int frameSize, byte[] data, int maxDataBytes);

        /// <summary>
        /// Frees an OpusEncoder allocated by opus_encoder_create().
        /// </summary>
        /// <param name="st">State to be freed.</param>
        void opus_encoder_destroy(IntPtr st);

        /// <summary>
        /// Perform a CTL function on an Opus encoder.
        /// </summary>
        /// <param name="st">Encoder state.</param>
        /// <param name="request"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        /// <remarks>
        /// From original documentation
        /// This and all remaining parameters should be replaced by one
        /// of the convenience macros in opus_genericctls or opus_encoderctls.
        /// </remarks>
        Error opus_encoder_ctl(IntPtr st, Ctl request, out int param);
        Error opus_encoder_ctl(IntPtr st, Ctl request, int param);

        #endregion // Encoder

        #region Decoder

        /// Opus Decoder
        /// 
        /// The decoding process also starts with creating a decoder
        /// state. This can be done with:
        /// int          error;
        /// OpusDecoder *dec;
        /// dec = opus_decoder_create(Fs, channels, &error);
        /// 
        /// where
        /// - Fs is the sampling rate and must be 8000, 12000, 16000, 24000, or 48000
        /// - channels is the number of channels (1 or 2)
        /// - error will hold the error code in case of failure (or #OPUS_OK on success)
        /// - the return value is a newly created decoder state to be used for decoding
        ///
        /// While opus_decoder_create() allocates memory for the state, it's also possible
        /// to initialize pre-allocated memory:
        /// 
        /// int          size;
        /// int          error;
        /// OpusDecoder *dec;
        /// size = opus_decoder_get_size(channels);
        /// dec = malloc(size);
        /// error = opus_decoder_init(dec, Fs, channels);
        /// 
        /// where opus_decoder_get_size() returns the required size for the decoder state. Note that
        /// future versions of this code may change the size, so no assuptions should be made about it.
        ///
        /// The decoder state is always continuous in memory and only a shallow copy is sufficient
        /// to copy it (e.g. memcpy())
        ///
        /// To decode a frame, opus_decode() or opus_decode_float() must be called with a packet of compressed audio data:
        /// frame_size = opus_decode(dec, packet, len, decoded, max_size, 0);
        /// where
        /// - packet is the byte array containing the compressed data
        /// - len is the exact number of bytes contained in the packet
        /// - decoded is the decoded audio data in opus_int16 (or float for opus_decode_float())
        /// - max_size is the max duration of the frame in samples (per channel) that can fit into the decoded_frame array
        ///
        /// opus_decode() and opus_decode_float() return the number of samples (per channel) decoded from the packet.
        /// If that value is negative, then an error has occurred. This can occur if the packet is corrupted or if the audio
        /// buffer is too small to hold the decoded audio.
        ///
        /// Opus is a stateful codec with overlapping blocks and as a result Opus
        /// packets are not coded independently of each other. Packets must be
        /// passed into the decoder serially and in the correct order for a correct
        /// decode. Lost packets can be replaced with loss concealment by calling
        /// the decoder with a null pointer and zero length for the missing packet.
        ///
        /// A single codec state may only be accessed from a single thread at
        /// a time and any required locking must be performed by the caller. Separate
        /// streams must be decoded with separate decoder states and can be decoded
        /// in parallel unless the library was compiled with NONTHREADSAFE_PSEUDOSTACK
        /// defined.
        /// <summary>
        /// Gets the size of an OpusDecoder structure.
        /// </summary>
        /// <param name="channels">
        /// Number of channels.
        /// This must be 1 or 2.
        /// </param>
        /// <returns>The size in bytes.</returns>
        int opus_decoder_get_size(int channels);

        /// <summary>
        /// Allocates and initializes a decoder state.
        /// </summary>
        /// <param name="fs">
        /// Sample rate to decode at (Hz).
        /// This must be one of 8000, 12000, 16000, 24000, or 48000.
        /// </param>
        /// <param name="channels">Number of channels (1 or 2) to decode</param>
        /// <param name="error">#OPUS_OK Success or opus_errorcodes</param>
        /// <returns>Decoder state</returns>
        /// <remarks>
        /// Internally Opus stores data at 48000 Hz, so that should be the default value for Fs.
        /// However, the decoder can efficiently decode to buffers
        /// at 8, 12, 16, and 24 kHz so if for some reason the caller cannot use
        /// data at the full sample rate, or knows the compressed data doesn't
        /// use the full frequency range, it can request decoding at a reduced
        /// rate. Likewise, the decoder is capable of filling in either mono or
        /// interleaved stereo pcm buffers, at the caller's request.
        /// </remarks>
        IntPtr opus_decoder_create(int fs, int channels, out Error error);

        /// <summary>
        /// Initializes a previously allocated decoder state.
        /// The state must be at least the size returned by opus_decoder_get_size().
        /// This is intended for applications which use their own allocator instead of malloc.
        /// To reset a previously initialized state, use the #OPUS_RESET_STATE CTL.
        /// </summary>
        /// <param name="st">Decoder state.</param>
        /// <param name="fs">
        /// Sampling rate to decode to (Hz).
        /// This must be one of 8000, 12000, 16000, 24000, or 48000.
        /// </param>
        /// <param name="channels">Number of channels (1 or 2) to decode</param>
        /// <returns>#OPUS_OK Success or opus_errorcodes</returns>
        Error opus_decoder_init(IntPtr st, int fs, int channels);

        /// <summary>
        /// Decode an Opus packet with floating point output.
        /// </summary>
        /// <param name="st">Decoder state</param>
        /// <param name="data">Input payload. Use a NULL pointer to indicate packet loss</param>
        /// <param name="len">Number of bytes in payload*</param>
        /// <param name="pcm">
        /// Output signal (interleaved if 2 channels).
        /// Length is frame_size*channels*sizeof(float)
        /// </param>
        /// <param name="frameSize">
        /// Number of samples per channel of available space in \a pcm.
        /// If this is less than the maximum packet duration (120ms; 5760 for 48kHz), this function will
        /// not be capable of decoding some packets.In the case of PLC(data==NULL) or FEC(decode_fec= 1),
        /// then frame_size needs to be exactly the duration of audio that is missing, otherwise the
        /// decoder will not be in the optimal state to decode the next incoming packet.For the PLC and
        /// FEC cases, frame_size <b>must</b> be a multiple of 2.5 ms.
        /// </param>
        /// <param name="decodeFec">
        /// Flag (0 or 1) to request that any in-band forward error correction data be decoded.
        /// If no such data is available, the frame is decoded as if it were lost.
        /// </param>
        /// <returns>Number of decoded samples or opus_errorcodes</returns>
        int opus_decode(IntPtr st, byte[] data, int len, short[] pcm, int frameSize, int decodeFec);

        /// <summary>
        /// Decode an Opus packet.
        /// </summary>
        /// <param name="st">Decoder state</param>
        /// <param name="data">Input payload. Use a NULL pointer to indicate packet loss</param>
        /// <param name="len">Number of bytes in payload*</param>
        /// <param name="pcm">
        /// Output signal (interleaved if 2 channels).
        /// Length is frame_size*channels*sizeof(opus_int16)
        /// </param>
        /// <param name="frameSize">
        /// Number of samples per channel of available space in \a pcm.
        /// If this is less than the maximum packet duration (120ms; 5760 for 48kHz), this function will
        /// not be capable of decoding some packets.In the case of PLC(data==NULL) or FEC(decode_fec= 1),
        /// then frame_size needs to be exactly the duration of audio that is missing, otherwise the
        /// decoder will not be in the optimal state to decode the next incoming packet.For the PLC and
        /// FEC cases, frame_size <b>must</b> be a multiple of 2.5 ms.
        /// </param>
        /// <param name="decodeFec">
        /// Flag (0 or 1) to request that any in-band forward error correction data be decoded.
        /// If no such data is available, the frame is decoded as if it were lost.
        /// </param>
        /// <returns>Number of decoded samples or opus_errorcodes</returns>
        int opus_decode_float(IntPtr st, byte[] data, int len, float[] pcm, int frameSize, int decodeFec);

        /// <summary>
        /// Perform a CTL function on an Opus decoder.
        /// </summary>
        /// <param name="st">Decoder state.</param>
        /// <param name="request"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        /// /// <remarks>
        /// From original documentation
        /// This and all remaining parameters should be replaced by one
        /// of the convenience macros in opus_genericctls or opus_decoderctls.
        /// </remarks>
        Error opus_decoder_ctl(IntPtr st, Ctl request, out int param);
        Error opus_decoder_ctl(IntPtr st, Ctl request, int param);
        
        /// <summary>
        /// Frees an OpusDecoder allocated by opus_decoder_create().
        /// </summary>
        /// <param name="st">State to be freed.</param>
        void opus_decoder_destroy(IntPtr st);

        #endregion // Decoder

        #region Packet

        /// <summary>
        /// Gets the number of samples of an Opus packet.
        /// </summary>
        /// <param name="packet">Opus packet</param>
        /// <param name="len">Length of packet</param>
        /// <param name="fs">
        /// Sampling rate in Hz.
        /// This must be a multiple of 400, or inaccurate results will be returned.
        /// </param>
        /// <returns>
        /// Number of samples
        /// OPUS_BAD_ARG Insufficient data was passed to the function
        /// OPUS_INVALID_PACKET The compressed data passed is corrupted or of an unsupported type
        /// </returns>
        int opus_packet_get_nb_samples(byte[] packet, int len, int fs);
        
        #endregion // Packet
    }
}