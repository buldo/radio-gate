namespace MumbleSharp.Voice
{
    public class VoiceStateChangedEventArgs
    {
        internal VoiceStateChangedEventArgs(UserVoiceState oldState, UserVoiceState newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        public UserVoiceState OldState { get; }
        public UserVoiceState NewState { get; }
    }
}