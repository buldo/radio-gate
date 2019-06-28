using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MumbleSharp.Voice.Codecs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MumbleSharp.Voice.Tests
{
    [TestClass]
    public class EncodingTest
    {
        [TestMethod]
        public void CheckEncodingRepeatability()
        {
            var encoded1 = EncodeFile(Path.Combine("Content","test.wav"));
            var encoded2 = EncodeFile(Path.Combine("Content","test.wav"));

            for (int i = 0; i < encoded1.Count ; i++)
            {
                CollectionAssert.AreEqual(encoded1[i], encoded2[i]);
            }
            ;
        }

        [TestMethod]
        public void PlayEncodedDecoded()
        {
            var encoded = EncodeFile(Path.Combine("Content", "test.wav"));

            var decoded = new List<byte[]>();
            var codec = new OpusCodec();
            foreach (var frame in encoded)
            {
                decoded.Add(codec.Decode(frame));
            }

            var bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(Constants.SAMPLE_RATE, 1))
            {
                BufferDuration = TimeSpan.FromMinutes(5)
            };
            var waveOut = new WaveOutEvent();
            waveOut.Init(bufferedWaveProvider);
            var evnt = new AutoResetEvent(false);
            waveOut.PlaybackStopped += (sender, args) => evnt.Set();
            waveOut.Play();

            foreach (var data in decoded)
            {
                bufferedWaveProvider.AddSamples(data, 0, data.Length);
            }

            evnt.WaitOne(TimeSpan.FromSeconds(30));
        }

        private List<byte[]> EncodeFile(string fileName)
        {
            var fileReader = new WaveFileReader(fileName);
            var resampler = new WdlResamplingSampleProvider(
                new StereoToMonoSampleProvider(new Pcm16BitToSampleProvider(fileReader)),
                Constants.SAMPLE_RATE);

            var resampledWaveProvider = resampler.ToWaveProvider16();

            var codec = new OpusCodec();
            var maxFrameSize = codec.PermittedEncodingFrameSizes.Max();
            var maxFrameSizeInBytes = maxFrameSize * 2;
            var ret = new List<byte[]>();
            var buffer = new byte[maxFrameSizeInBytes];
            while (resampledWaveProvider.Read(buffer, 0, maxFrameSizeInBytes) == maxFrameSizeInBytes)
            {
                ret.Add(codec.Encode(buffer));
            }

            return ret;
        }
    }
}
