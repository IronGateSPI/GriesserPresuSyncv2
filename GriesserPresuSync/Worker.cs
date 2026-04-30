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
    /// <summary>
    /// Worker de presupuestos. Mismo patrón que WorkerMallorquinas:
    ///   - Un MiGriesserApiController por worker
    ///   - Un GriesserSyncPresuController por iteración
    ///   - Loop con Task.Delay parametrizable + try/catch para que un fallo
    ///     puntual (red, BD) NO tumbe el servicio de Windows.
    /// </summary>
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly MiGriesserApiController _apiController;
        private readonly TimeSpan _delay;

        public Worker(
            ILogger<Worker> logger,
            IServiceScopeFactory serviceScopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _apiController = new MiGriesserApiController();

            // Intervalo configurable; por defecto 10s.
            var intervalSeconds = _configuration.GetValue<int?>("GriesserSyncSettings:SyncIntervalSeconds");
            _delay = intervalSeconds.HasValue
                ? TimeSpan.FromSeconds(intervalSeconds.Value)
                : TimeSpan.FromMilliseconds(10000);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker (presupuestos) iniciado, intervalo {0}s", _delay.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                try
                {
                    var presuController = new GriesserSyncPresuController(
                        _apiController, _logger, _serviceScopeFactory);
                    presuController.syncPresupuestos();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error en la iteración del Worker de presupuestos");
                }

                try
                {
                    await Task.Delay(_delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // SCM ha pedido parar. Salida limpia.
                    break;
                }
            }

            _logger.LogInformation("Worker (presupuestos) detenido");
        }
    }
}
