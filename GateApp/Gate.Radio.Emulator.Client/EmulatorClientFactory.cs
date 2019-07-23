using System;
using System.Net.Http;
using Gate.Radio.Emulator.Shared;
using Grpc.Core;
using Grpc.Net.Client;

namespace Gate.Radio.Emulator.Client
{
    public static class EmulatorClientFactory
    {
        public static EmulatorClient Create()
        {
            var httpClient = new HttpClient();
            // The port number(50051) must match the port of the gRPC server.
            httpClient.BaseAddress = new Uri($"https://localhost:{Defaults.Port}");
            return GrpcClient.Create<EmulatorClient>(httpClient);
        }
    }
}
