using System;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using NAudio.CoreAudioApi;
using NAudio.Wave;


namespace NAudio.Pulse.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            start:
            Console.WriteLine("Select option");
            Console.WriteLine("1. Play audio");
            Console.WriteLine("2. Record audio");
            Console.WriteLine("3. Loopback");

            var read = Console.ReadLine();
            if (!int.TryParse(read, out var option))
            {
                goto start;
            }

            switch (option)
            {
                    case 1:
                        PlayAudio();
                        break;
                    case 2:
                        RecordAudio();
                        break;
                    case 3:
                        Loopback();
                        break;
                    default:
                        goto start;
            }
        }

        private static void RecordAudio()
        {
            throw new NotImplementedException();
        }

        private static void PlayAudio()
        {
            bool playing = true;
            var fileReader = new WaveFileReader("test.wav");
            var paOut = new PulseAudioConnectionParameters(null, "MumbleSharpDemo", null, "Playback");
            var waveOut = new WaveOutPulse(paOut);
            waveOut.Init(fileReader);
            waveOut.PlaybackStopped += (sender, args) => { playing = false; };
            waveOut.Play();
            using (var src = new CancellationTokenSource())
            {
                src.Token.WaitHandle.WaitOne();
            }
        }

        private static void Loopback()
        {
            var _playBuffer = new BufferedWaveProvider(new WaveFormat(48000, 16, 1));
            var paIn = new PulseAudioConnectionParameters(null, "MumbleSharpDemo", null, "Record");
            IWaveIn _waveIn = new WaveInEventPulse(paIn)
            {
                BufferMilliseconds = 50,
                WaveFormat = new WaveFormat(48000, 16, 1)
            };
            _waveIn.DataAvailable += _waveIn_DataAvailable;

            var paOut = new PulseAudioConnectionParameters(null, "MumbleSharpDemo", null, "Playback");
            var _waveOut = new WaveOutPulse(paOut);
            _waveOut.Init(_playBuffer);


            var _startTime = DateTime.Now;
            ulong _bytesSent = 0;

            _waveOut.Play();
            _waveIn.StartRecording();

            var timer = new System.Timers.Timer {Interval = 1000};
            timer.Elapsed += _timer_Tick;

            timer.Start();
            while (true)
            {

            }

            void _timer_Tick(object sender, EventArgs e)
            {
                var timeDiff = DateTime.Now - _startTime;
                var bytesPerSecond = _bytesSent / timeDiff.TotalSeconds;
                Console.WriteLine("{0} Bps", bytesPerSecond);
            }

            void _waveIn_DataAvailable(object sender, WaveInEventArgs e)
            {
                _playBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
            }
        }
    }
}