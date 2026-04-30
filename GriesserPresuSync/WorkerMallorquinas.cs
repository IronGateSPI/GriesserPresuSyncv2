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
    /// Worker de mallorquinas. Mismo patrón que Worker.cs (presupuestos):
    ///   - Un MiGriesserMallorquinasApiController por worker
    ///   - Un GriesserSyncMallorController por iteración
    ///   - Loop con Task.Delay
    /// </summary>
    public class WorkerMallorquinas : BackgroundService
    {
        private readonly ILogger<WorkerMallorquinas> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private MiGriesserMallorquinasApiController _apiController;
        private readonly TimeSpan _delay;

        public WorkerMallorquinas(
            ILogger<WorkerMallorquinas> logger,
            IServiceScopeFactory serviceScopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;

            // Construimos los settings desde IConfiguration; si no hay sección, valores por defecto.
            var settings = new MallorquinasSyncSettings();
            var section = _configuration.GetSection("MallorquinasSyncSettings");
            if (section != null && section.Exists())
            {
                var apiUrl = section["ApiUrl"];
                if (!string.IsNullOrWhiteSpace(apiUrl)) settings.ApiUrl = apiUrl;
            }

            _apiController = new MiGriesserMallorquinasApiController(settings);

            // Intervalo: por defecto 10s (igual que presupuestos). Si en config hay
            // SyncIntervalSeconds, se respeta; si no, fallback 10000 ms.
            var intervalSeconds = _configuration.GetValue<int?>("MallorquinasSyncSettings:SyncIntervalSeconds");
            _delay = intervalSeconds.HasValue
                ? TimeSpan.FromSeconds(intervalSeconds.Value)
                : TimeSpan.FromMilliseconds(10000);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("WorkerMallorquinas running at: {time}", DateTimeOffset.Now);
                try
                {
                    var mallorController = new GriesserSyncMallorController(_apiController, _logger, _serviceScopeFactory);
                    mallorController.syncMallorquinas();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error en la iteración del WorkerMallorquinas");
                }

                try
                {
                    await Task.Delay(_delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
    /*public class BudgetSyncWorker : BackgroundService
    {
        private readonly BudgetSyncProcess _syncProcess;
        private readonly ILogger<BudgetSyncWorker> _logger;
        private readonly IConfiguration _configuration;
        private TimeSpan _syncInterval;
        private int _daysBack;

        public BudgetSyncWorker(
            BudgetSyncProcess syncProcess,
            ILogger<BudgetSyncWorker> logger,
            IConfiguration configuration)
        {
            _syncProcess = syncProcess;
            _logger = logger;
            _configuration = configuration;

            // Leer configuración
            var intervalHours = _configuration.GetValue<int>("MallorquinasSyncSettings:SyncIntervalHours", 1);
            _syncInterval = TimeSpan.FromHours(intervalHours);
            _daysBack = _configuration.GetValue<int>("MallorquinasSyncSettings:DaysBack", 30);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Budget Sync Worker iniciado. Intervalo: {Interval} horas", _syncInterval.TotalHours);

            // Ejecutar inmediatamente al iniciar
            await ExecuteSyncAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_syncInterval, stoppingToken);
                    await ExecuteSyncAsync();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Sincronización cancelada");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inesperado en el worker");
                }
            }

            _logger.LogInformation("Budget Sync Worker detenido");
        }

        private async Task ExecuteSyncAsync()
        {
            try
            {
                _logger.LogInformation("Iniciando sincronización de budgets de mallorquinas...");

                await _syncProcess.SyncBudgetsAsync(_daysBack);

                _logger.LogInformation("Sincronización de budgets completada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la sincronización de budgets");
            }
        }
    }
    */
}