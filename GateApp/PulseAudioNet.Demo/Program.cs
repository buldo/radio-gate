using System;
using System.IO;
using PulseAudioNet.Api;

namespace PulseAudioNet.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var api = PulseAudioFactory.GetApi();

            var ss = new SampleSpec
            {
                Format = SampleFormat.S16Le,
                Rate = 44100,
                Channels = 2
            };
            /* The Sample format to use */

            ChannelMap? map = null;
            BufferAttr? bufferAttr = null;

            var s = api.pa_simple_new(
                null,
                "PulseAudioNet.Demo",
                StreamDirection.Playback,
                null,
                "Playback",
                ref ss,
                ref map,
                ref bufferAttr,
                out var createError);
            if (s == IntPtr.Zero)
            {
                Console.WriteLine($"pa_simple_new() failed: {createError}");
                goto finish;
            }



//#if 0
//        pa_usec_t latency;
//        if ((latency = pa_simple_get_latency(s, &error)) == (pa_usec_t) -1) {
//            fprintf(stderr, __FILE__": pa_simple_get_latency() failed: %s\n", pa_strerror(error));
//            goto finish;
//        }
//        fprintf(stderr, "%0.0f usec    \r", (float)latency);
//#endif
            /* Read some data ... */

            var data = File.ReadAllBytes("test.wav");
            const int bufferSize = 1024;
            var buffer = new byte[bufferSize];
            for (int i = 0; i < data.Length/bufferSize; i++)
            {
                Array.Copy(data, i*bufferSize,buffer,0,bufferSize);

                if (api.pa_simple_write(s, buffer, (UIntPtr)bufferSize, out var writeError) < 0)
                {
                    Console.WriteLine($"pa_simple_write() failed: {writeError}");
                    goto finish;
                }
            }

            /* Make sure that every single sample was played */
            if (api.pa_simple_drain(s, out var error) < 0)
            {
                Console.WriteLine($"pa_simple_drain() failed: {error}");
                goto finish;
            }

            finish:
            if (s != IntPtr.Zero)
            {
                api.pa_simple_free(s);
            }

        }
    }
}