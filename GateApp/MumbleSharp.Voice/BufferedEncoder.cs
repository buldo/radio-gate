using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using MumbleSharp.Voice.Codecs;

namespace MumbleSharp.Voice
{
    internal class BufferedEncoder
    {
        private readonly Channel<byte[]> _readyFrames = Channel.CreateUnbounded<byte[]>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

        private readonly Pipe _incoming = new Pipe();

        public BufferedEncoder()
        {
            Task.Factory.StartNew(EncodeAsync, TaskCreationOptions.LongRunning);
        }

        public void AddPcm(Span<byte> pcm)
        {
            _incoming.Writer.Write(pcm);
        }

        public ChannelReader<byte[]> EncodedFrames => _readyFrames.Reader;

        private async Task EncodeAsync()
        {
            var codec = new OpusCodec();
            //Get the codec

            //How many bytes can we fit into the larget frame?
            var maxBytes = codec.PermittedEncodingFrameSizes.Max() * sizeof(ushort);

            while (true)
            {
                var readResult = await _incoming.Reader.ReadAsync();
                var buffer = readResult.Buffer;
                if (buffer.Length < maxBytes)
                {
                    continue;
                }

                byte[] pcm;
                if (buffer.IsSingleSegment)
                {
                    var end = buffer.GetPosition(maxBytes - 1);
                    pcm = buffer.Slice(0, end).ToArray();
                    _incoming.Reader.AdvanceTo(buffer.Start, end);
                }
                else
                {
                    pcm = buffer.ToArray().AsSpan(0, maxBytes).ToArray();
                    _incoming.Reader.AdvanceTo(buffer.Start, buffer.GetPosition(maxBytes));
                }

                var encoded = codec.Encode(pcm.AsSpan());

                await _readyFrames.Writer.WriteAsync(encoded);
            }
        }
    }
}
