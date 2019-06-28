using System;
using NAudio.Pulse;
using NAudio.Wave;

namespace MumbleSharp.Voice
{
    public class Recorder
    {
        private readonly VoiceService _voiceService;
        private readonly IWaveIn _sourceStream;

        private bool _isRecording;

        public Recorder(VoiceService voiceService)
        {
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

            _sourceStream.DataAvailable += VoiceDataAvailable;
        }

        public void StartCapture()
        {
            if (_isRecording)
            {
                return;
            }

            _isRecording = true;
            _sourceStream.StartRecording();
        }

        public void StopCapture()
        {
            if (!_isRecording)
            {
                return;
            }

            _isRecording = false;
            _sourceStream.StopRecording();
        }

        private void VoiceDataAvailable(object sender, WaveInEventArgs e)
        {
            if (!_isRecording)
                return;

            _voiceService.SendVoice(e.Buffer.AsSpan(0, e.BytesRecorded));
        }
    }
}
