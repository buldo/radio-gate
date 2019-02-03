using System;
using AdvancedDLSupport;
using PulseAudioNet.Api;

namespace PulseAudioNet
{
    public static class PulseAudioFactory
    {
        private static readonly object CreateLock = new object();
        private static IPulseAudioSimpleApi _simpleApi;

        public static IPulseAudioSimpleApi GetApi()
        {
            if (_simpleApi == null)
            {
                lock (CreateLock)
                {
                    if (_simpleApi == null)
                    {
                        _simpleApi = NativeLibraryBuilder.Default.ActivateInterface<IPulseAudioSimpleApi>("libpulse-simple.so.0");
                    }
                }
            }

            return _simpleApi;
        }
    }
}