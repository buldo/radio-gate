using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace Gate.Daemon
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder();
            
            var host = hostBuilder.Build();
            await host.RunAsync();
        }
    }
}
