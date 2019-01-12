using MumbleSharp.Audio.Codecs;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Timers;

namespace MumbleSharp.Audio
{
    /// <summary>
    /// Buffers up encoded audio packets and provides a constant stream of sound (silence if there is no more audio to decode)
    /// Can decode only OPUS
    /// </summary>
    public class AudioDecodingBuffer
    {
        private readonly Timer _transmissionTimer = new Timer(100);
        private long _lastDecodedSequence = 0;
        private IVoiceCodec _codec;

        public AudioDecodingBuffer()
        {
            _transmissionTimer.Elapsed += TransmissionTimerOnElapsed;
            WaveProvider = new BufferedWaveProvider(new WaveFormat((int)Constants.SAMPLE_RATE, (int)Constants.SAMPLE_BITS, 1));
        }
        
        public BufferedWaveProvider WaveProvider { get; }
        
        /// <summary>
        /// Add a new packet of encoded data
        /// </summary>
        /// <param name="sequence">Sequence number of this packet</param>
        /// <param name="data">The encoded audio packet</param>
        /// <param name="codec">The codec to use to decode this packet</param>
        public void AddEncodedPacket(long sequence, byte[] data, IVoiceCodec codec)
        {
            if (_codec == null)
            {
                _codec = codec;
            }

            //If the next seq we expect to decode comes after this packet we've already missed our opportunity!
            if (_lastDecodedSequence > sequence)
            {
                return;
            }

            _transmissionTimer.Stop();
            _lastDecodedSequence = sequence;

            var d = _codec.Decode(data);
            WaveProvider.AddSamples(d, 0, d.Length);
            if (!_transmissionTimer.Enabled)
            {
                _transmissionTimer.Start();
            }
        }

        private void TransmissionTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            _lastDecodedSequence = -1;
        }
    }
}
