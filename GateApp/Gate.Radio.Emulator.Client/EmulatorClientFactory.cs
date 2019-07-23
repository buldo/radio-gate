using System;
using System.Net.Http;
using Gate.Radio.Emulator.Shared;
using Grpc.Net.Client;

namespace Gate.Radio.Emulator.Client
{
    public class EmulatorClientFactory
    {
        public RadioEmulator.RadioEmulatorBase Create()
        {
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
                true);
            var httpClient = new HttpClient();
            // The port number(50051) must match the port of the gRPC server.
            httpClient.BaseAddress = new Uri($"http://localhost:{Defaults.Port}");
            return GrpcClient.Create<RadioEmulator.RadioEmulatorBase>(httpClient);
        }
    }
}
