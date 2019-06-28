using System;
using Microsoft.Extensions.Logging;
using NAudio.Pulse;
using NAudio.Wave;

namespace MumbleSharp.Voice
{
    public class Recorder
    {
        private ILogger _logger;
        private readonly VoiceService _voiceService;
        private readonly IWaveIn _sourceStream;

        private bool _isRecording;

        public Recorder(VoiceService voiceService, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Recorder>();

            _voiceService = voiceService;

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                _sourceStream = new WaveInEventPulse(new PulseAudioConnectionParameters(null, "MumbleSharpDemo", null, "Record"));
            }
            else
            {
                _sourceStream = new WaveInEvent();
            }

            _sourceStream.WaveFormat = new WaveFormat(Constants.SAMPLE_RATE, Constants.SAMPLE_BITS, Constants.CHANNELS);

            _sourceStream.DataAvailable += VoiceDataAvailableAsync;
        }

        public void StartCapture()
        {
            _logger.LogInformation($"{nameof(StartCapture)} invoked");
            if (_isRecording)
            {
                return;
            }

            _isRecording = true;
            _sourceStream.StartRecording();
        }

        public void StopCapture()
        {
            _logger.LogInformation($"{nameof(StopCapture)} invoked");
            if (!_isRecording)
            {
                return;
            }

            _isRecording = false;
            _sourceStream.StopRecording();
        }

        private async void VoiceDataAvailableAsync(object sender, WaveInEventArgs e)
        {
            if (!_isRecording)
                return;

            //_logger.LogTrace($"Captured {e.BytesRecorded} bytes");
            
            _voiceService.SendVoice(e.Buffer.AsSpan(0, e.BytesRecorded));
        }
    }
}
