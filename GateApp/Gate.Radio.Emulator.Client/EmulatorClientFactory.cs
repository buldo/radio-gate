using System;
using System.Net.Http;
using Grpc.Net.Client;

namespace Gate.Radio.Emulator.Client
{
    public class EmulatorClientFactory
    {
        public void Create()
        {
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
                true);
            var httpClient = new HttpClient();
            // The port number(50051) must match the port of the gRPC server.
            httpClient.BaseAddress = new Uri("http://localhost:50051");
            c = GrpcClient.Create<RadioEmulator.RadioEmulatorBase>(httpClient);
        }

        class MyClass : RadioEmulator.RadioEmulatorBase
        {
            
        }
    }
}
