using System;
using System.Collections.Generic;
using System.Linq;
using Gate.Radio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gate.Daemon
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IRadioStation>(new RadioEmulator());
                    services.AddHostedService<Worker>();
                });

            builder.Build().Run();
        }
    }
}
