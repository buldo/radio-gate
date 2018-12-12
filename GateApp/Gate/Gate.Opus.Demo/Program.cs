using System;
using AdvancedDLSupport;

namespace Gate.Opus.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var opus = NativeLibraryBuilder.Default.ActivateInterface<IOpusApi>("opus");
            var enc = opus.opus_encoder_create(8000, 1, 2048, out var err);
            
            Console.WriteLine("Hello World!");
        }
    }
}
