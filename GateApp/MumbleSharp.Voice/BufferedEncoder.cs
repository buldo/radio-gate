using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MumbleSharp.Voice.Codecs;

namespace MumbleSharp.Voice
{
    internal class BufferedEncoder
    {
        private readonly ILogger<BufferedEncoder> _logger;

        private readonly Channel<byte[]> _readyFrames = Channel.CreateUnbounded<byte[]>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

        private readonly Channel<byte[]> _incomingPcm = Channel.CreateUnbounded<byte[]>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

        private readonly Pipe _pcmPipe = new Pipe();

        public BufferedEncoder(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BufferedEncoder>();
            Task.Factory.StartNew(() => EncodeAsync(_pcmPipe.Reader, _readyFrames.Writer, _logger), TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(() => MovePcmAsync(), TaskCreationOptions.LongRunning);
        }

        public ChannelReader<byte[]> EncodedFrames => _readyFrames.Reader;

        public void AddPcm(Span<byte> pcm)
        {
            _incomingPcm.Writer.TryWrite(pcm.ToArray());
        }

        private static async Task EncodeAsync(PipeReader pipeReader, ChannelWriter<byte[]> readyFramesWriter, ILogger<BufferedEncoder> logger)
        {
            var codec = new OpusCodec();
            //Get the codec

            //How many bytes can we fit into the larget frame?
            var blockSize = codec.PermittedEncodingFrameSizes.Max() * sizeof(ushort);

            while (true)
            {
                var readResult = await pipeReader.ReadAsync();
                var buffer = readResult.Buffer;
                if (buffer.Length < blockSize)
                {
                    pipeReader.AdvanceTo(buffer.Start, buffer.End);
                    continue;
                }

                var block = buffer.Slice(buffer.Start, blockSize);
                var encoded = codec.Encode(block.IsSingleSegment ? block.First.Span : block.ToArray().AsSpan());
                pipeReader.AdvanceTo(block.End);
                await readyFramesWriter.WriteAsync(encoded);
                logger.LogTrace("Encoded written");
            }
        }

        private async Task MovePcmAsync()
        {
            while (true)
            {
                var data = await _incomingPcm.Reader.ReadAsync();
                _pcmPipe.Writer.Write(data);
                await _pcmPipe.Writer.FlushAsync();
            }
        }
    }
}
