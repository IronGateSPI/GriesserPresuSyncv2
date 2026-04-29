using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiGriesserMallorquinas;

namespace GriesserPresuSync.Services
{
    public class BudgetSyncWorker : BackgroundService
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
}