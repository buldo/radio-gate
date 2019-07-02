using System;
using System.Collections.Generic;
using System.Text;

namespace MumbleSharp.Voice.Codecs
{
    public static class MumbleOpusHelper
    {
        static MumbleOpusHelper()
        {
            //float[] frameSizes = {2.5f, 5, 10, 20, 40, 60};
            float[] frameSizes = {2.5f, 5, 10, 20};

            var permittedFrameSizes = new int[frameSizes.Length];
            for (var i = 0; i < frameSizes.Length; i++)
            {
                permittedFrameSizes[i] = (int) ((Constants.SAMPLE_RATE / 1000f) * frameSizes[i]);
            }

            PermittedEncodingFrameSizes = permittedFrameSizes;
        }

        public static IReadOnlyList<int> PermittedEncodingFrameSizes { get; }

    }
}