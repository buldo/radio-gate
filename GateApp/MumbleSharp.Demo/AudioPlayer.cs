using System;
using NAudio.Pulse;
using NAudio.Wave;

namespace MumbleSharp.Demo
{
    internal class AudioPlayer
    {
        private readonly IWavePlayer _playbackDevice;

        private AudioPlayer(uint id)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                _playbackDevice =
                    new WaveOutPulse(
                        new PulseAudioConnectionParameters(null, "MumbleSharpDemo", null, $"Playback{id}"));
            }
            else
            {
                _playbackDevice = new WaveOutEvent();
            }
        }

        public AudioPlayer(IWaveProvider provider, uint id)
            : this(id)
        {
            _playbackDevice.Init(provider);
            _playbackDevice.Play();

            _playbackDevice.PlaybackStopped += (sender, args) => Console.WriteLine("Playback stopped: " + args.Exception);
        }
    }
}