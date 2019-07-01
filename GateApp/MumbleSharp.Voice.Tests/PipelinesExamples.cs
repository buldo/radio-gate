using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MumbleSharp.Voice.Tests
{
    [TestClass]
    public class PipelinesExamples
    {
        private int sliceSize = 5760;

        [TestMethod]
        public async Task SliceTest()
        {
            var data = GetWaveData();
            var expected = Slice(data);
            var actual = await SliceWithPipeAsync(data);

            for (int i = 0; i < expected.Count; i++)
            {
                CollectionAssert.AreEqual(expected[i], actual[i]);
            }
        }

        private async Task<List<byte[]>> SliceWithPipeAsync(List<byte> data)
        {
            var ret = new List<byte[]>();
            var pipe = new Pipe();
            await Task.WhenAll(
                WriteToPipeAsync(data, pipe.Writer, sliceSize / 4),
                EncodeFromPipeAsync(pipe.Reader, sliceSize, ret)
            );

            return ret;
        }

        private async Task WriteToPipeAsync(List<byte> data, PipeWriter pipeWriter, int blockSize)
        {
            for (int i = 0; i < data.Count; i = i + blockSize)
            {
                pipeWriter.Write(data.GetRange(i, blockSize).ToArray());
                await pipeWriter.FlushAsync();
                Thread.Sleep(2);
            }

            pipeWriter.Complete();
        }

        private async Task EncodeFromPipeAsync(PipeReader pipeReader, int blockSize, List<byte[]> ret)
        {
            while (true)
            {
                var readResult = await pipeReader.ReadAsync();
                if (readResult.IsCompleted)
                {
                    break;
                }

                var buffer = readResult.Buffer;

                if (buffer.Length < blockSize)
                {
                    pipeReader.AdvanceTo(buffer.Start, buffer.End);
                    continue;
                }

                var data = buffer.Slice(buffer.Start, blockSize).ToArray();
                ret.Add(data);
                pipeReader.AdvanceTo(buffer.GetPosition(blockSize));
            }
        }

        private List<byte[]> Slice(List<byte> data)
        {
            var ret = new List<byte[]>();
            for (int i = 0; i < data.Count; i = i+sliceSize)
            {
                ret.Add(data.GetRange(i, sliceSize).ToArray());
            }

            return ret;
        }

        private List<byte> GetWaveData()
        {
            var size = sliceSize * 905;
            var ret = new List<byte>(size);

            byte b = 0;
            for (int i = 0; i < size; i++)
            {
                b++;
                ret.Add(b);
            }

            return ret;
        }
    }
}
