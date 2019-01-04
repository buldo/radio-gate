using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text;
using Gate.Opus.Api;

namespace Gate.Opus
{
    public class OpusDecoder : IDisposable
    {
        private readonly IOpusApi _api;
        private IntPtr _decoderState;
        private bool _disposed;
        private readonly int _channels;

        internal OpusDecoder(IOpusApi api, int samplingRate, int channels)
        {
            _api = api;
            _channels = channels;

            _decoderState = api.opus_decoder_create(samplingRate, channels, out var error);
            if (error != Error.OK)
            {
                throw new Exception("Exception occured while creating encoder");
            }
        }

        /// <summary>
        /// Gets or sets the bitrate setting of the encoding.
        /// </summary>
        public int Bitrate
        {
            get
            {
                CheckDisposed();
                int bitrate;
                var ret = _api.opus_decoder_ctl(_decoderState, Ctl.GET_BITRATE_REQUEST, out bitrate);
                if (ret < 0)
                    throw new Exception("Encoder error - " + (ret).ToString());
                return bitrate;
            }
            set
            {
                CheckDisposed();
                var ret = _api.opus_decoder_ctl(_decoderState, Ctl.SET_BITRATE_REQUEST, value);
                if (ret != Error.OK)
                    throw new Exception("Encoder error - " + (ret).ToString());
            }
        }

        public int Channels
        {
            get
            {
                CheckDisposed();
                return _channels;
            }
        }

        /// <summary>
        /// Gets or sets the size of memory allocated for reading encoded data.
        /// 4000 is recommended.
        /// </summary>
        public int MaxDataBytes { get; set; } = 4000;

        /// <summary>
        /// Gets or sets whether forward error correction is enabled or not.
        /// </summary>
        public bool ForwardErrorCorrection { get; set; }

        /// <summary>
        /// Produces PCM samples from Opus encoded data.
        /// </summary>
        /// <param name="data">Opus encoded data to decode, null for dropped packet.</param>
        /// <returns>PCM audio samples.</returns>
        public ReadOnlySpan<short> Decode(ReadOnlySpan<byte> data, int frameSize)
        {
            CheckDisposed();

            var decoded = new short[MaxDataBytes];
            int length = 0;
            if (data != null)
            {
                length = _api.opus_decode(_decoderState, data.ToArray(), data.Length, decoded, frameSize, 0);
            }
            else
            {
                length = _api.opus_decode(_decoderState, null, 0, decoded, frameSize, (ForwardErrorCorrection) ? 1 : 0);
            }
          
            if (length < 0)
                throw new Exception("Decoding failed - " + ((Error)length).ToString());

            return decoded.AsSpan(0, length);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~OpusDecoder()
        {
            ReleaseUnmanagedResources();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OpusEncoder));
            }
        }

        private void ReleaseUnmanagedResources()
        {
            if (_decoderState != IntPtr.Zero)
            {
                _api.opus_decoder_destroy(_decoderState);
                _decoderState = IntPtr.Zero;
            }
        }
    }
}
