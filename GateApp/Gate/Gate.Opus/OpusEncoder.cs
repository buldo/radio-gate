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
                int bitrate;
                var ret = _api.opus_encoder_ctl(_encoderState, Ctl.GET_BITRATE_REQUEST, out bitrate);
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

        /// <summary>
        /// Produces Opus encoded audio from PCM samples.
        /// </summary>
        /// <param name="inputPcmSamples">PCM samples to encode.</param>
        /// <param name="sampleLength">How many bytes to encode.</param>
        /// <param name="encodedLength">Set to length of encoded audio.</param>
        /// <returns>Opus encoded audio buffer.</returns>
        public unsafe byte[] Encode(short[] inputPcmSamples, int sampleLength, out int encodedLength)
        {
            CheckDisposed();

            int frames = FrameCount(inputPcmSamples);
            IntPtr encodedPtr;
            byte[] encoded = new byte[MaxDataBytes];
            int length = 0;
            fixed (byte* benc = encoded)
            {
                encodedPtr = new IntPtr((void*)benc);
                length = _api.opus_encode(_encoderState, inputPcmSamples, frames, encoded, sampleLength);
            }
            encodedLength = length;
            if (length < 0)
                throw new Exception("Encoding failed - " + ((Error)length).ToString());

            return encoded;
        }

        /// <summary>
        /// Helper method to determine how many bytes are required for encoding to work.
        /// </summary>
        /// <param name="frameCount">Target frame size.</param>
        /// <returns></returns>
        public int GetFrameByteCount(int frameCount)
        {
            int bitrate = 16;
            int bytesPerSample = (bitrate / 8) * Channels;
            return frameCount * bytesPerSample;
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

        /// <summary>
        /// Determines the number of frames in the PCM samples.
        /// </summary>
        /// <param name="pcmSamples"></param>
        /// <returns></returns>
        private int FrameCount(short[] pcmSamples)
        {
            //  seems like bitrate should be required
            int bitrate = 16;
            int bytesPerSample = (bitrate / 8) * Channels;
            return pcmSamples.Length / (bytesPerSample * 2);
        }
    }
}
