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
            _stateService.OnStateChangedAction = OnStateChangedAction;
        }

        private void OnStateChangedAction()
        {
            TxState = _stateService.TxState;
        }

        public TxState TxState
        {
            get => _txState;
            private set => SetProperty(ref _txState, value, nameof(TxState));
        }
    }
}
