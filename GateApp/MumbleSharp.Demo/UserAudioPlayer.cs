using MumbleSharp.Audio;
using MumbleSharp.Audio.Codecs;
using NAudio.Wave;

namespace MumbleSharp.Demo
{
    internal class UserAudioPlayer
    {
        private readonly AudioPlayer _audioPlayer;
        private readonly AudioDecodingBuffer _audioDecodingBuffer;
        private readonly OpusCodec _codec = new OpusCodec();

        public UserAudioPlayer(uint userId)
        {

            var waveProvider = new BufferedWaveProvider(new WaveFormat((int)Constants.SAMPLE_RATE, (int)Constants.SAMPLE_BITS, 1));
            _audioDecodingBuffer = new AudioDecodingBuffer(waveProvider);
            _audioPlayer = new AudioPlayer(waveProvider, userId);
        }

        public void ProcessEncodedVoice(byte[] data, long sequence)
        {
            _audioDecodingBuffer.AddEncodedPacket(sequence, data, _codec);
        }
    }
}
