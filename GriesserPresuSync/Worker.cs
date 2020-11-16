using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GriesserPresuSync.Controllers;
using GriesserPresuSync.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GriesserPresuSync
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly GriesserSyncSettings _syncSettings;
        private MiGriesserApiController _apiController;

        public Worker(ILogger<Worker> logger, GriesserSyncSettings settings)
        {
            _logger = logger;
            _syncSettings = settings;
            _apiController = new MiGriesserApiController(_syncSettings);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                GriesserSyncPresuController presuController = new GriesserSyncPresuController(_apiController, _logger);
                presuController.syncPresupuestos();
                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
