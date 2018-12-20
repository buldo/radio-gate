using System;
using System.Runtime.InteropServices;
using System.Timers;
using AdvancedDLSupport;
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
        static int _segmentFrames;
        static int _bytesPerSegment;
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

            _waveOut.Play();
            _waveIn.StartRecording();
            
            var opusFactory = new OpusFactory();
            _startTime = DateTime.Now;
            _bytesSent = 0;
            _segmentFrames = 960;
            _encoder = opusFactory.CreateEncoder(48000, 1, Application.VoIP);
            _encoder.Bitrate = 8192;
            _decoder = opusFactory.CreateDecoder(48000, 1);
            _bytesPerSegment = _encoder.GetFrameByteCount(_segmentFrames);

            
            if (_timer == null)
            {
                _timer = new Timer();
                _timer.Interval = 1000;
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
            var recordedShorts = MemoryMarshal.Cast<byte,short>(recorderBytes);

            var soundBuffer = new short[recordedShorts.Length + _notEncodedBuffer.Length];
            _notEncodedBuffer.CopyTo(soundBuffer,0);
            recordedShorts.CopyTo(soundBuffer.AsSpan(_notEncodedBuffer.Length));
            
            int byteCap = _bytesPerSegment;
            int segmentCount = (int)Math.Floor((decimal)soundBuffer.Length / byteCap);
            int segmentsEnd = segmentCount * byteCap;
            int notEncodedCount = soundBuffer.Length - segmentsEnd;
            _notEncodedBuffer = new byte[notEncodedCount];
            for (int i = 0; i < notEncodedCount; i++)
            {
                _notEncodedBuffer[i] = soundBuffer[segmentsEnd + i];
            }

            for (int i = 0; i < segmentCount; i++)
            {
                var segment = new short[byteCap];
                for (int j = 0; j < segment.Length; j++)
                    segment[j] = soundBuffer[(i * byteCap) + j];
                int len;
                byte[] buff = _encoder.Encode(segment, segment.Length, out len);
                _bytesSent += (ulong)len;
                var dec = _decoder.Decode(buff, len, out len);
                _playBuffer.AddSamples(MemoryMarshal.Cast<short, byte>(new ReadOnlySpan<short>(dec)).ToArray(), 0, len);
            }
        }
    }
}
