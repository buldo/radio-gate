using System;
using System.Threading.Tasks;
using Gate.Radio;
using Gate.Radio.Emulator.Client;

namespace Gate.Daemon
{
    internal class RadioEmulator : IRadioStation
    {
        private readonly Lazy<EmulatorClient> _client =
            new Lazy<EmulatorClient>(EmulatorClientFactory.Create);

        protected EmulatorClient Client => _client.Value;

        public RadioState State { get; }
        public event EventHandler<RadioStateChangedEventArgs> StateChanged;

        public void StartTx()
        {
            Client.StartTx();
        }

        public void StopTx()
        {
            Client.StopTx();
        }
    }
}