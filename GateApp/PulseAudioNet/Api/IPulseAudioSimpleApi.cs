using System;
using JetBrains.Annotations;

namespace PulseAudioNet.Api
{
    public interface IPulseAudioSimpleApi
    {
        IntPtr pa_simple_new(
            [CanBeNull] string server,
            [NotNull] string name,
            StreamDirection direction,
            [CanBeNull] string device,
            [NotNull] string streamName,
            ref SampleSpec spec,
            [CanBeNull] ref ChannelMap? channelMap,
            [CanBeNull] ref BufferAttr? bufferAttributes,
            out int error);

        void pa_simple_free(IntPtr s);

        int pa_simple_write(IntPtr s, byte[] data, UIntPtr bytes, out int error);

        int pa_simple_drain(IntPtr s, out int error);

        int pa_simple_read(IntPtr s, byte[] data, UIntPtr bytes, out int error);

        ulong pa_simple_get_latency(IntPtr s, out int error);

        int pa_simple_flush(IntPtr s, out int error);
    }
}