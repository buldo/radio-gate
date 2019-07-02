using System;
using System.Timers;
using MumbleSharp.Voice.Codecs;
using NAudio.Pulse;
using NAudio.Wave;

namespace MumbleSharp.Voice
{
    internal class UserAudioPlayer : IDisposable
    {
        private readonly Timer _transmissionTimer = new Timer(100);
        private readonly MumbleOpusDecoder _codec = new MumbleOpusDecoder();
        private readonly BufferedWaveProvider _waveProvider;
        private readonly IWavePlayer _player;
        private long _lastDecodedSequence = -1;

        public UserAudioPlayer(uint userId)
        {
            _transmissionTimer.Elapsed += TransmissionTimerOnElapsed;
            _waveProvider = new BufferedWaveProvider(new WaveFormat(Constants.SAMPLE_RATE, Constants.SAMPLE_BITS, 1));

            _player = CreateWavePlayer(userId);
            _player.Init(_waveProvider);
            _player.Play();
            _player.PlaybackStopped += (sender, args) => Console.WriteLine("Playback stopped: " + args.Exception);
        }

        public void ProcessEncodedVoice(byte[] data, long sequence)
        {
            //If the next seq we expect to decode comes after this packet we've already missed our opportunity!
            if (_lastDecodedSequence > sequence)
            {
                return;
            }

            _transmissionTimer.Stop();
            _lastDecodedSequence = sequence;

            var d = _codec.Decode(data);
            _waveProvider.AddSamples(d, 0, d.Length);
            if (!_transmissionTimer.Enabled)
            {
                _transmissionTimer.Start();
            }
        }

        private static IWavePlayer CreateWavePlayer(uint id)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return new WaveOutPulse(
                    new PulseAudioConnectionParameters(null, "MumbleSharpDemo", null, $"Playback{id}"));
            }
            else
            {
                return new WaveOutEvent();
            }
        }

        private void TransmissionTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            _lastDecodedSequence = -1;
        }

        public void Dispose()
        {
            _transmissionTimer.Dispose();
            _codec.Dispose();
            _player.Dispose();
        }
    }
}
