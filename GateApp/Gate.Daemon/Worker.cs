using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gate.Radio;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gate.Daemon
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IRadioStation _radioStation;

        public Worker(ILogger<Worker> logger, IRadioStation radioStation)
        {
            _logger = logger;
            _radioStation = radioStation;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _radioStation.StopTx();
                await Task.Delay(1000, stoppingToken);
                _radioStation.StartTx();
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
