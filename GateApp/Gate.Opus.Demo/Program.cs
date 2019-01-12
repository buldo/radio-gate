using System;
using System.Runtime.InteropServices;
using System.Timers;
using Gate.Opus.Api;
using NAudio.Wave;

namespace Gate.Opus.Demo
{
    class Program
    {

        static WaveInEvent _waveIn;
        static WaveOutEvent _waveOut;
        static BufferedWaveProvider _playBuffer;
        static OpusEncoder _encoder;
        static OpusDecoder _decoder;
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
            _encoder = OpusFactory.CreateEncoder(48000, 1, Application.VoIP);
            _encoder.Bitrate = 8192;
            _decoder = OpusFactory.CreateDecoder(48000, 1);

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

        static short[] _notEncodedBuffer = new short[0];

        static void _waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            var recorderBytes = e.Buffer.AsSpan(0, e.BytesRecorded);
            var recordedShorts = MemoryMarshal.Cast<byte, short>(recorderBytes);
            //_playBuffer.AddSamples(MemoryMarshal.Cast<short, byte>(recordedShorts).ToArray(), 0, e.BytesRecorded);
            //return;

            var soundBuffer = new short[recordedShorts.Length + _notEncodedBuffer.Length];
            _notEncodedBuffer.CopyTo(soundBuffer,0);
            recordedShorts.CopyTo(soundBuffer.AsSpan(_notEncodedBuffer.Length));
            
            int segmentCount = soundBuffer.Length / ShortsPerSegment;

            var willNotEncoded = soundBuffer.AsSpan(segmentCount * ShortsPerSegment);
            _notEncodedBuffer = willNotEncoded.ToArray();

            for (int i = 0; i < segmentCount; i++)
            {
                
                var segment = soundBuffer.AsSpan(i* ShortsPerSegment, ShortsPerSegment);
                var buff = _encoder.Encode(segment, segment.Length);
                _bytesSent += (ulong)buff.Length;
                var dec = _decoder.Decode(buff, segment.Length);
                _playBuffer.AddSamples(MemoryMarshal.Cast<short, byte>(dec).ToArray(), 0, dec.Length * 2);
            }
        }
    }
}
