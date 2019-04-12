using System;
using System.Collections.Concurrent;

namespace MumbleSharp.Voice
{
    public class VoiceSubsystem
    {
        private readonly ConcurrentDictionary<uint, UserAudioPlayer> _audioBuffers = new ConcurrentDictionary<uint, UserAudioPlayer>();

        internal void UserStateProcessor(object obj)
        {
            throw new NotImplementedException();
        }

        internal void UserRemoveProcessor(object obj)
        {
            throw new NotImplementedException();
        }
    }
}