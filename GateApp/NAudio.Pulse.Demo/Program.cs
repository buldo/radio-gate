using System;
using System;
using System.Runtime.InteropServices;
using System.Timers;
using NAudio.Wave;


namespace NAudio.Pulse.Demo
{
    class Program
    {
        static WaveInEvent _waveIn;
        static WaveOutEvent _waveOut;
        static BufferedWaveProvider _playBuffer;
        const int ShortsPerSegment = 960;
        static ulong _bytesSent;
        static DateTime _startTime;
        static Timer _timer = null;

        static void Main(string[] args)
        {
            _waveIn = new WaveInEvent
            {
                BufferMilliseconds = 50,
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(48000, 16, 1)
            };
            _waveIn.DataAvailable += _waveIn_DataAvailable;

            _playBuffer = new BufferedWaveProvider(new WaveFormat(48000, 16, 1));

            _waveOut = new WaveOutEvent
            {
                DeviceNumber = 0
            };
            _waveOut.Init(_playBuffer);


            _startTime = DateTime.Now;
            _bytesSent = 0;
//            _encoder = OpusFactory.CreateEncoder(48000, 1, Application.VoIP);
//            _encoder.Bitrate = 8192;
//            _decoder = OpusFactory.CreateDecoder(48000, 1);

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