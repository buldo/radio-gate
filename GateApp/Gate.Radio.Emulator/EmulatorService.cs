using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace Gate.Radio.Emulator
{
    class EmulatorService : RadioEmulator.RadioEmulatorBase
    {
        private readonly RadioStateService _stateService;

        public EmulatorService(RadioStateService stateService)
        {
            _stateService = stateService;
            
        }

        public override Task<Empty> StartTx(Empty request, ServerCallContext context)
        {
            _stateService.StartTx();
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> StopTx(Empty request, ServerCallContext context)
        {
            _stateService.StopTx();
            return Task.FromResult(new Empty());
        }
    }
}
