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
    /// Worker que drena la cola IG_CRM_ClientesPendientes y envía cada
    /// cliente al CRM Griesser por PUT (sync_sage_distributor).
    ///
    /// Mismo patrón que Worker.cs y WorkerMallorquinas.cs:
    ///   - Un ClientesCrmApiController por worker (HttpClient compartido estático).
    ///   - Un GriesserSyncClientesController por iteración (estado por scope EF).
    ///   - Loop con Task.Delay configurable + try/catch para que un fallo
    ///     puntual (red, BD) NO tumbe el Windows Service.
    /// </summary>
    public class WorkerClientesCrm : BackgroundService
    {
        private readonly ILogger<WorkerClientesCrm> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ClientesCrmSyncSettings _settings;
        private readonly ClientesCrmApiController _apiController;
        private readonly TimeSpan _delay;
        private readonly bool _enabled;

        public WorkerClientesCrm(
            ILogger<WorkerClientesCrm> logger,
            IServiceScopeFactory serviceScopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _settings = LoadSettings(configuration);
            _enabled = _settings.EnableExport;

            _apiController = new ClientesCrmApiController(_settings);

            var seconds = _settings.SyncIntervalSeconds <= 0 ? 30 : _settings.SyncIntervalSeconds;
            _delay = TimeSpan.FromSeconds(seconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled)
            {
                _logger.LogInformation("WorkerClientesCrm DESACTIVADO por configuración (EnableExport=false). El worker no procesará la cola.");
                // Esperamos pasivamente la cancelación para no salir del proceso.
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
                return;
            }

            _logger.LogInformation("WorkerClientesCrm iniciado, intervalo {0}s", _delay.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("WorkerClientesCrm running at: {time}", DateTimeOffset.Now);

                try
                {
                    var ctrl = new GriesserSyncClientesController(
                        _apiController, _logger, _serviceScopeFactory, _settings);
                    await ctrl.SyncClientesAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error en iteración del WorkerClientesCrm");
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

            _logger.LogInformation("WorkerClientesCrm detenido");
        }

        /// <summary>
        /// Carga la sección "ClientesCrmSyncSettings" de appsettings con
        /// valores por defecto del POCO si la sección no existe.
        /// </summary>
        private static ClientesCrmSyncSettings LoadSettings(IConfiguration cfg)
        {
            var s = new ClientesCrmSyncSettings();
            var sec = cfg.GetSection("ClientesCrmSyncSettings");
            if (sec != null && sec.Exists())
            {
                var apiUrl = sec["ApiUrl"];
                if (!string.IsNullOrWhiteSpace(apiUrl)) s.ApiUrl = apiUrl;

                if (bool.TryParse(sec["EnableExport"], out var en)) s.EnableExport = en;
                if (int.TryParse(sec["SyncIntervalSeconds"], out var iv)) s.SyncIntervalSeconds = iv;
                if (int.TryParse(sec["MaxIntentos"], out var mi)) s.MaxIntentos = mi;
                if (short.TryParse(sec["CodigoEmpresa"], out var ce)) s.CodigoEmpresa = ce;
                if (int.TryParse(sec["HttpTimeoutSeconds"], out var ht)) s.HttpTimeoutSeconds = ht;
                if (int.TryParse(sec["FacturasHistoricoMeses"], out var fh) && fh > 0) s.FacturasHistoricoMeses = fh;

                if (bool.TryParse(sec["EnableImport"], out var ei)) s.EnableImport = ei;
                if (int.TryParse(sec["ImportHour"], out var ih)) s.ImportHour = ih;
            }
            return s;
        }
    }
}
