using System;
using System.Threading;
using System.Threading.Tasks;
using GriesserPresuSync.Controllers;
using GriesserPresuSync.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GriesserPresuSync
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private MiGriesserApiController _apiController;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _apiController = new MiGriesserApiController();
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                GriesserSyncPresuController presuController = new GriesserSyncPresuController(_apiController, _logger, _serviceScopeFactory);
                presuController.syncPresupuestos();
                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
