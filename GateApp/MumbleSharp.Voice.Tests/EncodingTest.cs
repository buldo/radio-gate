using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            var encoded1 = EncodeFile();
            var encoded2 = EncodeFile();

            for (int i = 0; i < encoded1.Count ; i++)
            {
                CollectionAssert.AreEqual(encoded1[i], encoded2[i]);
            }
        }

        [TestMethod]
        [Ignore]
        public void PlayEncodedDecoded()
        {
            var encoded = EncodeFile();

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

        [TestMethod]
        public async Task EncodeWithPipe()
        {
            var expected = EncodeFile();
            var actual = await EncodeFileWithPipeAsync();

            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                CollectionAssert.AreEqual(expected[i], actual[i]);
            }
        }

        private static async Task<List<byte[]>> EncodeFileWithPipeAsync()
        {
            var resampledWaveProvider = GetWaveProvider();

            var pipe = new Pipe();
            var ret = new List<byte[]>();
            var codec = new OpusCodec();
            var maxFrameSizeInBytes = codec.PermittedEncodingFrameSizes.Max() * 2;

            await Task.WhenAll(
                WriteToPipeAsync(resampledWaveProvider, pipe.Writer, maxFrameSizeInBytes),
                EncodeFromPipeAsync(codec, pipe.Reader, maxFrameSizeInBytes, ret)
            );
            return ret;
        }

        private static async Task WriteToPipeAsync(IWaveProvider waveProvider, PipeWriter pipeWriter, int blockSize)
        {
            while(true)
            {
                var buffer = new byte[blockSize*2];
                if (waveProvider.Read(buffer, 0, blockSize) != blockSize)
                {
                    break;
                }

                if (waveProvider.Read(buffer, blockSize, blockSize) != blockSize)
                {
                    pipeWriter.Write(buffer.AsSpan(0, blockSize));
                    await pipeWriter.FlushAsync();
                    break;
                }

                pipeWriter.Write(buffer);
                await pipeWriter.FlushAsync();
            }

            pipeWriter.Complete();
        }

        private static async Task EncodeFromPipeAsync(OpusCodec codec, PipeReader pipeReader, int blockSize, List<byte[]> ret)
        {
            while (true)
            {
                var readResult = await pipeReader.ReadAsync();
                var buffer = readResult.Buffer;
                if (readResult.IsCompleted && buffer.Length < blockSize)
                {
                    break;
                }

                if (buffer.Length < blockSize)
                {
                    pipeReader.AdvanceTo(buffer.Start, buffer.End);
                    continue;
                }

                var block = buffer.Slice(buffer.Start, blockSize);
                if (block.IsSingleSegment)
                {
                    ret.Add(codec.Encode(block.FirstSpan));
                }
                else
                {
                    ret.Add(codec.Encode(block.ToArray()));
                }

                pipeReader.AdvanceTo(block.End);
            }
        }

        private static List<byte[]> EncodeFile()
        {
            var resampledWaveProvider = GetWaveProvider();

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

        private static IWaveProvider GetWaveProvider()
        {
            return new WdlResamplingSampleProvider(
                new StereoToMonoSampleProvider(
                    new Pcm16BitToSampleProvider(new WaveFileReader(Path.Combine("Content", "test.wav")))),
                Constants.SAMPLE_RATE).ToWaveProvider16();
        }

    }
}
