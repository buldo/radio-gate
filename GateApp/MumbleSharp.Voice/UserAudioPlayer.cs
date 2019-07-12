using System;
using System.Collections;
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
        private UserVoiceState _state;

        public UserAudioPlayer(uint userId)
        {
            _transmissionTimer.Elapsed += TransmissionTimerOnElapsed;
            _waveProvider = new BufferedWaveProvider(new WaveFormat(Constants.SAMPLE_RATE, Constants.SAMPLE_BITS, 1));

            _player = CreateWavePlayer(userId);
            _player.Init(_waveProvider);
            _player.Play();
            _player.PlaybackStopped += (sender, args) => Console.WriteLine("Playback stopped: " + args.Exception);
        }

        public UserVoiceState State
        {
            get => _state;
            private set
            {
                if(_state != value)
                {
                    var oldState = _state;
                    _state = value;
                    StateChanged?.Invoke(this, new VoiceStateChangedEventArgs(oldState, value));

                    if (value == UserVoiceState.Idle)
                    {
                        _lastDecodedSequence = -1;
                    }

                    if(value == UserVoiceState.Tx)
                    {
                        if (!_transmissionTimer.Enabled)
                        {
                            _transmissionTimer.Start();
                        }
                    }
                }
            }
        }

        public event EventHandler<VoiceStateChangedEventArgs> StateChanged;

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
            State = UserVoiceState.Tx;
            // TODO: добавить сюда вычитывание стоп бита
        }

        public void Dispose()
        {
            _transmissionTimer.Dispose();
            _codec.Dispose();
            _player.Dispose();
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
            State = UserVoiceState.Idle;
        }
    }
}
