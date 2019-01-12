using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text;
using Gate.Opus.Api;

namespace Gate.Opus
{
    public class OpusEncoder : IDisposable
    {
        private readonly IOpusApi _api;
        private IntPtr _encoderState;
        private bool _disposed;
        private readonly int _channels;


        internal OpusEncoder(IOpusApi api, int samplingRate, int channels, Application application)
        {
            _api = api;
            _channels = channels;
            _encoderState = api.opus_encoder_create(samplingRate, channels, application, out var error);
            if ((Error)error != Error.OK)
            {
                throw new Exception("Exception occured while creating encoder");
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
        /// Gets or sets the bitrate setting of the encoding.
        /// </summary>
        public int Bitrate
        {
            get
            {
                CheckDisposed();
                var ret = _api.opus_encoder_ctl(_encoderState, Ctl.GET_BITRATE_REQUEST, out var bitrate);
                if (ret < 0)
                    throw new Exception("Encoder error - " + (ret).ToString());
                return bitrate;
            }
            set
            {
                CheckDisposed();
                var ret = _api.opus_encoder_ctl(_encoderState, Ctl.SET_BITRATE_REQUEST, value);
                if (ret != Error.OK)
                    throw new Exception("Encoder error - " + (ret).ToString());
            }
        }

        public bool IsForwardErrorCorrectionEnabled
        {
            get
            {
                CheckDisposed();
                var ret = _api.opus_encoder_ctl(_encoderState, Ctl.GET_INBAND_FEC_REQUEST, out var isFecEnabled);
                if (ret < 0)
                    throw new Exception("Encoder error - " + (ret).ToString());
                return isFecEnabled > 0;
            }
            set
            {
                CheckDisposed();
                var ret = _api.opus_encoder_ctl(_encoderState, Ctl.SET_INBAND_FEC_REQUEST, Convert.ToInt32(value));
                if (ret != Error.OK)
                    throw new Exception("Encoder error - " + (ret).ToString());
            }
        }

        /// <summary>
        /// Produces Opus encoded audio from PCM samples.
        /// </summary>
        /// <param name="inputPcmSamples">PCM samples to encode.</param>
        /// <param name="frameSize">How many bytes to encode.</param>
        /// <returns>Opus encoded audio buffer.</returns>
        public ReadOnlySpan<byte> Encode(Span<short> inputPcmSamples, int frameSize)
        {
            CheckDisposed();

            var encoded = new byte[MaxDataBytes];
            var length = _api.opus_encode(_encoderState, inputPcmSamples.ToArray(), frameSize, encoded, frameSize);
            
            if (length < 0)
                throw new Exception("Encoding failed - " + ((Error)length).ToString());

            return encoded.AsSpan(0, length);
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

        ~OpusEncoder()
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
            if (_encoderState != IntPtr.Zero)
            {
                _api.opus_encoder_destroy(_encoderState);
                _encoderState = IntPtr.Zero;
            }
        }
    }
}
