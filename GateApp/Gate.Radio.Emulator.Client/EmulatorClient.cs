using System.Threading.Tasks;
using Grpc.Core;

namespace Gate.Radio.Emulator.Client
{
    public class EmulatorClient : RadioEmulator.RadioEmulatorClient
    {
        public EmulatorClient(CallInvoker callInvoker)
            : base(callInvoker)
        {
        }

        public void StartTx()
        {
            base.StartTx(new Empty());
        }

        public void StopTx()
        {
            base.StopTx(new Empty());
        }
    }
}