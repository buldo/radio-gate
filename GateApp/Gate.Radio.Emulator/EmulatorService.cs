using System;
using System.Collections.Generic;
using System.Text;

namespace Gate.Radio.Emulator
{
    class EmulatorService : RadioEmulator.RadioEmulatorBase
    {
        private readonly RadioStateService _stateService;

        public EmulatorService(RadioStateService stateService)
        {
            _stateService = stateService;
        }
    }
}
