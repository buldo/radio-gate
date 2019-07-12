using System;
using System.Collections.Generic;
using System.Text;
using Prism.Mvvm;

namespace Gate.Radio.Emulator
{
    class MainWindowViewModel : BindableBase
    {
        private readonly RadioStateService _stateService;

        public MainWindowViewModel(RadioStateService stateService)
        {
            _stateService = stateService;
        }

        public 
    }
}
