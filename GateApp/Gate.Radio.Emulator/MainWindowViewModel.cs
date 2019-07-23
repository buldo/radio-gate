using System;
using Prism.Mvvm;

namespace Gate.Radio.Emulator
{
    class MainWindowViewModel : BindableBase
    {
        private readonly RadioStateService _stateService;
        private TxState _txState;
        public MainWindowViewModel(RadioStateService stateService)
        {
            _stateService = stateService;
            _stateService.StateChanged += StateServiceOnStateChanged;
        }

        public TxState TxState
        {
            get => _txState;
            private set => SetProperty(ref _txState, value, nameof(TxState));
        }

        private void StateServiceOnStateChanged(object sender, EventArgs e)
        {
            TxState = _stateService.TxState;
        }
    }
}
