using System;

namespace Gate.Radio
{
    // Возможно стоит добавить отдельные флаги Tx и Rx. Rx по таймеру перепроверять
    public class RadioStation : IRadioStation
    {
        private RadioState _state;

        public RadioStation()
        {

        }

        public RadioState State
        {
            get => _state;
            private set
            {
                if(_state == value)
                {
                    return;
                }

                var oldState = _state;
                _state = value;
                StateChanged?.Invoke(this, new RadioStateChangedEventArgs(oldState, value));
            }
        }

        public event EventHandler<RadioStateChangedEventArgs> StateChanged;

        public void StartTransceiving()
        {

            State = RadioState.Tx;
        }

        public void StopTransceiving()
        {
            State = RadioState.Idle; // Сюда проверку. Может кто-то уже разговаривает
        }
    }
}
