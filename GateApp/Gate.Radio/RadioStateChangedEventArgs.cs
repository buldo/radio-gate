namespace Gate.Radio
{
    public class RadioStateChangedEventArgs
    {
        internal RadioStateChangedEventArgs(RadioState oldState, RadioState newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        public RadioState OldState { get; }

        public RadioState NewState { get; }
    }
}
