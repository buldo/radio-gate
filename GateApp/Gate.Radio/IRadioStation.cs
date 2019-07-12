using System;

namespace Gate.Radio
{
    public interface IRadioStation
    {
        RadioState State { get; }

        event EventHandler<RadioStateChangedEventArgs> StateChanged;

        void StartTransceiving();
        void StopTransceiving();
    }
}