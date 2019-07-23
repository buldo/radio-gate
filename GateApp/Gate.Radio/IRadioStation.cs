using System;
using System.Threading.Tasks;

namespace Gate.Radio
{
    public interface IRadioStation
    {
        RadioState State { get; }

        event EventHandler<RadioStateChangedEventArgs> StateChanged;

        void StartTx();
        void StopTx();
    }
}