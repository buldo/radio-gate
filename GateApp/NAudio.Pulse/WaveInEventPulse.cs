using System;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using PulseAudioNet;
using PulseAudioNet.Api;

namespace NAudio.Pulse
{
    /// <summary>
    /// Recording using waveIn api with event callbacks.
    /// Use this for recording in non-gui applications
    /// Events are raised as recorded buffers are made available
    /// </summary>
    public class WaveInEventPulse: IWaveIn
    {
        private readonly PulseAudioConnectionParameters _connection;
        private readonly IPulseAudioSimpleApi _api;
        private volatile CaptureState _captureState;
        private IntPtr _connectionHandle;

        private readonly AutoResetEvent callbackEvent;
        private readonly SynchronizationContext syncContext;

        /// <summary>
        /// Prepares a Wave input device for recording
        /// </summary>
        public WaveInEventPulse(PulseAudioConnectionParameters connection)
        {
            _connection = connection;
            _api = PulseAudioFactory.GetApi();
            callbackEvent = new AutoResetEvent(false);
            syncContext = SynchronizationContext.Current;
            BufferMilliseconds = 100;
            WaveFormat = new WaveFormat(8000, 8, 1);
            _captureState = CaptureState.Stopped;
        }

        /// <summary>
        /// Indicates recorded data is available
        /// </summary>
        public event EventHandler<WaveInEventArgs> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        /// <summary>
        /// Milliseconds for the buffer. Recommended value is 100ms
        /// </summary>
        public int BufferMilliseconds { get; set; }

        /// <summary>
        /// WaveFormat we are recording in
        /// </summary>
        public WaveFormat WaveFormat { get; set; }

        /// <summary>
        /// Start recording
        /// </summary>
        public void StartRecording()
        {
            if (_captureState != CaptureState.Stopped)
                throw new InvalidOperationException("Already recording");
            OpenWaveInDevice();
            _captureState = CaptureState.Starting;
            ThreadPool.QueueUserWorkItem((state) => RecordThread(), null);
        }

        /// <summary>
        /// Stop recording
        /// </summary>
        public void StopRecording()
        {
            if (_captureState != CaptureState.Stopped)
            {
                _captureState = CaptureState.Stopping;
            }
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_captureState != CaptureState.Stopped)
                {
                    StopRecording();
                }

                CloseWaveInDevice();
            }
        }

        private void OpenWaveInDevice()
        {
            CloseWaveInDevice();

            var sampleSpec = FormatConverter.Convert(WaveFormat);
            ChannelMap? channelMap = null;
            BufferAttr? bufferAttributes = null;
            _connectionHandle = _api.pa_simple_new(
                _connection.ServerName,
                _connection.ApplicationName,
                StreamDirection.Record,
                _connection.Device,
                _connection.StreamName,
                ref sampleSpec,
                ref channelMap,
                ref bufferAttributes,
                out var error
                );
            if (_connectionHandle == IntPtr.Zero)
            {
                throw new Exception("Not able to create recording pulseAudio connection");
            }
        }

        private void RecordThread()
        {
            Exception exception = null;
            try
            {
                DoRecording();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                _captureState = CaptureState.Stopped;
                RaiseRecordingStoppedEvent(exception);
            }
        }

        private void DoRecording()
        {
            _captureState = CaptureState.Capturing;

            while (_captureState == CaptureState.Capturing)
            {
                int bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
                if (bufferSize % WaveFormat.BlockAlign != 0)
                {
                    bufferSize -= bufferSize % WaveFormat.BlockAlign;
                }

                var buffer = new byte[bufferSize];
                if (_api.pa_simple_read(_connectionHandle, buffer, (UIntPtr)bufferSize, out var error) < 0)
                {
                    throw new Exception("PulseAudio read error");
                }

                DataAvailable?.Invoke(this, new WaveInEventArgs(buffer, bufferSize));
            }
        }

        private void RaiseRecordingStoppedEvent(Exception e)
        {
            var handler = RecordingStopped;
            if (handler != null)
            {
                if (syncContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }

        private void CloseWaveInDevice()
        {
            if (_connectionHandle != IntPtr.Zero)
            {
                _api.pa_simple_free(_connectionHandle);
            }
        }
    }
}