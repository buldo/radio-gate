using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using PulseAudioNet;
using PulseAudioNet.Api;

namespace NAudio.Pulse
{
    public class WaveOutPulse : IWavePlayer, IDisposable
    {
        private readonly PulseAudioConnectionParameters _connection;
        private readonly IPulseAudioSimpleApi _api;
        private volatile PlaybackState playbackState;
        private IntPtr _connectionHandle;
        private IWaveProvider _waveStream;
        private int _bufferSize;
        private byte[] _buffer;

        private readonly object waveOutLock;
        private readonly SynchronizationContext syncContext;

        /// <summary>
        /// Indicates playback has stopped automatically
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// Gets or sets the desired latency in milliseconds
        /// Should be set before a call to Init
        /// </summary>
        public int DesiredLatency { get; set; }

        /// <summary>
        /// Gets or sets the number of buffers used
        /// Should be set before a call to Init
        /// </summary>
        public int NumberOfBuffers { get; set; }

        /// <summary>
        /// Gets or sets the device number
        /// Should be set before a call to Init
        /// This must be between -1 and <see>DeviceCount</see> - 1.
        /// -1 means stick to default device even default device is changed
        /// </summary>
        public int DeviceNumber { get; set; } = -1;

        /// <summary>
        /// Opens a WaveOut device
        /// </summary>
        public WaveOutPulse(PulseAudioConnectionParameters connection)
        {
            _connection = connection;
            _api = PulseAudioFactory.GetApi();

            syncContext = SynchronizationContext.Current;
            if (syncContext != null &&
                ((syncContext.GetType().Name == "LegacyAspNetSynchronizationContext") ||
                (syncContext.GetType().Name == "AspNetSynchronizationContext")))
            {
                syncContext = null;
            }

            // set default values up
            DesiredLatency = 300;
            NumberOfBuffers = 2;

            waveOutLock = new object();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initialises the WaveOut device
        /// </summary>
        /// <param name="waveProvider">WaveProvider to play</param>
        public void Init(IWaveProvider waveProvider)
        {
            if (playbackState != PlaybackState.Stopped)
            {
                throw new InvalidOperationException("Can't re-initialize during playback");
            }

            if (_connectionHandle != IntPtr.Zero)
            {
                // normally we don't allow calling Init twice, but as experiment, see if we can clean up and go again
                // try to allow reuse of this waveOut device
                // n.b. risky if Playback thread has not exited
                DisposeBuffers();
                CloseWaveOut();
            }

            _waveStream = waveProvider;
            _bufferSize =
                waveProvider.WaveFormat.ConvertLatencyToByteSize(
                    (DesiredLatency + NumberOfBuffers - 1) / NumberOfBuffers);
            _buffer = new byte[_bufferSize];

            lock (waveOutLock)
            {
                var sampleSpec = FormatConverter.Convert(_waveStream.WaveFormat);
                ChannelMap? channelMap = null;
                BufferAttr? bufferAttr = null;
                _connectionHandle = _api.pa_simple_new(
                    _connection.ServerName,
                    _connection.ApplicationName,
                    StreamDirection.Playback,
                    _connection.Device,
                    _connection.StreamName,
                    ref sampleSpec,
                    ref channelMap,
                    ref bufferAttr,
                    out var error
                );

                if (_connectionHandle == IntPtr.Zero)
                {
                    throw new Exception("pa_simple_new error");
                }
            }

            playbackState = PlaybackState.Stopped;
        }

        public float Volume { get; set; }

        /// <summary>
        /// Start playing the audio from the WaveStream
        /// </summary>
        public void Play()
        {
            if (_waveStream == null)
            {
                throw new InvalidOperationException("Must call Init first");
            }
            if (playbackState == PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Playing;
                //Task.Run(() => PlaybackThread());
                ThreadPool.QueueUserWorkItem(state => PlaybackThread(), null);
            }
        }

        private void PlaybackThread()
        {
            Exception exception = null;
            try
            {
                DoPlayback();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                playbackState = PlaybackState.Stopped;
                // we're exiting our background thread
                RaisePlaybackStoppedEvent(exception);
            }
        }

        private void DoPlayback()
        {
            while (playbackState != PlaybackState.Stopped)
            {
                // requeue any buffers returned to us
                if (playbackState == PlaybackState.Playing)
                {
                    var readed = _waveStream.Read(_buffer, 0, _bufferSize);
                    if (readed > 0)
                    {
                        if (_api.pa_simple_write(_connectionHandle, _buffer, (UIntPtr) readed, out var error) != 0)
                        {
                            throw new Exception("Fail to write to pulse audio");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stop and reset the WaveOut device
        /// </summary>
        public void Stop()
        {
            if (playbackState != PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Stopped; // set this here to avoid a problem with some drivers whereby
            }
        }

        /// <summary>
        /// Gets a <see cref="Wave.WaveFormat"/> instance indicating the format the hardware is using.
        /// </summary>
        public WaveFormat OutputWaveFormat
        {
            get { return _waveStream.WaveFormat; }
        }

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState
        {
            get { return playbackState; }
        }


        #region Dispose Pattern

        /// <summary>
        /// Closes this WaveOut device
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Closes the WaveOut device and disposes of buffers
        /// </summary>
        /// <param name="disposing">True if called from <see>Dispose</see></param>
        protected void Dispose(bool disposing)
        {
            Stop();

            if (disposing)
            {
                DisposeBuffers();
            }

            CloseWaveOut();
        }

        private void CloseWaveOut()
        {
            lock (waveOutLock)
            {
                if (_connectionHandle != IntPtr.Zero)
                {
                    _api.pa_simple_free(_connectionHandle);
                    _connectionHandle = IntPtr.Zero;
                }
            }
        }

        private void DisposeBuffers()
        {
            if (_connectionHandle != IntPtr.Zero)
            {
                if (_api.pa_simple_flush(_connectionHandle, out var error) != 0)
                {
                    throw new Exception("Not able to flush");
                }
            }
        }

        /// <summary>
        /// Finalizer. Only called when user forgets to call <see>Dispose</see>
        /// </summary>
        ~WaveOutPulse()
        {
            Dispose(false);
            Debug.Assert(false, "WaveOutEvent device was not closed");
        }

        #endregion

        private void RaisePlaybackStoppedEvent(Exception e)
        {
            var handler = PlaybackStopped;
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
    }
}