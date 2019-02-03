using System;
using System;
using System.Runtime.InteropServices;
using System.Timers;
using NAudio.CoreAudioApi;
using NAudio.Wave;


namespace NAudio.Pulse.Demo
{
    class Program
    {
        static IWaveIn _waveIn;
        static WaveOutPulse _waveOut;
        static BufferedWaveProvider _playBuffer;
        const int ShortsPerSegment = 960;
        static ulong _bytesSent;
        static DateTime _startTime;
        static Timer _timer = null;

        static void Main(string[] args)
        {
            var paIn = new PulseAudioConnectionParameters(null, "MumbleSharpDemo", null, "Record");
            _waveIn = new WaveInEventPulse(paIn)
            {
                BufferMilliseconds = 50,
                WaveFormat = new WaveFormat(48000, 16, 1)
            };
            _waveIn.DataAvailable += _waveIn_DataAvailable;

            _playBuffer = new BufferedWaveProvider(new WaveFormat(48000, 16, 1));

            var paOut = new PulseAudioConnectionParameters(null, "MumbleSharpDemo", null, "Playback");
            _waveOut = new WaveOutPulse(paOut);
            _waveOut.Init(_playBuffer);


            _startTime = DateTime.Now;
            _bytesSent = 0;

            _waveOut.Play();
            _waveIn.StartRecording();

            if (_timer == null)
            {
                _timer = new Timer {Interval = 1000};
                _timer.Elapsed += _timer_Tick;
            }
            _timer.Start();
            while (true)
            {

            }
        }

        static void _timer_Tick(object sender, EventArgs e)
        {
            var timeDiff = DateTime.Now - _startTime;
            var bytesPerSecond = _bytesSent / timeDiff.TotalSeconds;
            Console.WriteLine("{0} Bps", bytesPerSecond);
        }

        static void _waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            _playBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }
    }
}