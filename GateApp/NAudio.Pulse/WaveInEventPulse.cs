using System;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Mixer;
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
        private const int BufferMilliseconds = 100;
        private readonly PulseAudioConnectionParameters _connection;
        private readonly IPulseAudioSimpleApi _api;
        private IntPtr _connectionHandle;

        private readonly AutoResetEvent callbackEvent;
        private readonly SynchronizationContext syncContext;
        private IntPtr waveInHandle;
        private volatile CaptureState captureState;

        /// <summary>
        /// Indicates recorded data is available
        /// </summary>
        public event EventHandler<WaveInEventArgs> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        /// <summary>
        /// Prepares a Wave input device for recording
        /// </summary>
        public WaveInEventPulse(
            PulseAudioConnectionParameters connection
            )
        {
            _connection = connection;
            _api = PulseAudioFactory.GetApi();
            callbackEvent = new AutoResetEvent(false);
            syncContext = SynchronizationContext.Current;
            WaveFormat = new WaveFormat(8000, 8, 1);
            captureState = CaptureState.Stopped;
        }

        /// <summary>
        /// The device number to use
        /// </summary>
        public int DeviceNumber { get; set; }

        private void OpenWaveInDevice()
        {
            CloseWaveInDevice();

            var sampleSpec = new SampleSpec
            {
                Rate = (uint)WaveFormat.SampleRate,
                Channels = (byte)WaveFormat.Channels,
                Format = FormatConverter.Convert(WaveFormat.Encoding)
            };
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

        /// <summary>
        /// Start recording
        /// </summary>
        public void StartRecording()
        {
            if (captureState != CaptureState.Stopped)
                throw new InvalidOperationException("Already recording");
            OpenWaveInDevice();
            captureState = CaptureState.Starting;
            ThreadPool.QueueUserWorkItem((state) => RecordThread(), null);
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
                captureState = CaptureState.Stopped;
                RaiseRecordingStoppedEvent(exception);
            }
        }

        private void DoRecording()
        {
            captureState = CaptureState.Capturing;
            foreach (var buffer in buffers)
            {
                if (!buffer.InQueue)
                {
                    buffer.Reuse();
                }
            }
            while (captureState == CaptureState.Capturing)
            {
                _api.pa_simple_read(_connectionHandle)
                if (callbackEvent.WaitOne())
                {
                    // requeue any buffers returned to us
                    foreach (var buffer in buffers)
                    {
                        if (buffer.Done)
                        {
                            if (buffer.BytesRecorded > 0)
                            {
                                DataAvailable?.Invoke(this, new WaveInEventArgs(buffer.Data, buffer.BytesRecorded));
                            }

                            if (captureState == CaptureState.Capturing)
                            {
                                buffer.Reuse();
                            }
                        }
                    }
                }
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
        /// <summary>
        /// Stop recording
        /// </summary>
        public void StopRecording()
        {
            if (captureState != CaptureState.Stopped)
            {
                captureState = CaptureState.Stopping;
            }
        }
        /// <summary>
        /// WaveFormat we are recording in
        /// </summary>
        public WaveFormat WaveFormat { get; set; }

        /// <summary>
        /// Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (captureState != CaptureState.Stopped)
                {
                    StopRecording();
                }

                CloseWaveInDevice();
            }
        }

        private void CloseWaveInDevice()
        {
            if (_connectionHandle != IntPtr.Zero)
            {
                _api.pa_simple_free(_connectionHandle);
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
    }
}