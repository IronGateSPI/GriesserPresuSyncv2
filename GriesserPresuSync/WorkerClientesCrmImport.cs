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
    /// Worker NOCTURNO: descarga los partners del CRM y actualiza dos
    /// campos en dbo.Clientes:
    ///   - zzTarget        ← target_de_ventas
    ///   - zzTipoCliente   ← partner_type
    ///
    /// Ejecuta una sola vez al día a la hora indicada en
    /// ClientesCrmSyncSettings.ImportHour (default 03:00 AM).
    ///
    /// Diseño del scheduler interno:
    ///   - Calculamos el siguiente DateTime de ejecución.
    ///   - Si la hora del día indicada ya pasó hoy, se programa para mañana.
    ///   - Task.Delay con el TimeSpan resultante. Si supera el máximo de
    ///     Task.Delay (24.85 días) lo dividimos en bloques (no es nuestro
    ///     caso: como mucho son ~24h, pero queda blindado por si alguien
    ///     pone ImportHour absurda).
    ///   - Tras ejecutar, vuelve a calcular el siguiente fire.
    ///
    /// Si EnableImport=false, el worker arranca, loguea y se queda dormido
    /// esperando cancelación (no consume CPU).
    /// </summary>
    public class WorkerClientesCrmImport : BackgroundService
    {
        private readonly ILogger<WorkerClientesCrmImport> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ClientesCrmSyncSettings _settings;
        private readonly ClientesCrmApiController _apiController;

        public WorkerClientesCrmImport(
            ILogger<WorkerClientesCrmImport> logger,
            IServiceScopeFactory serviceScopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _settings = LoadSettings(configuration);
            _apiController = new ClientesCrmApiController(_settings);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.EnableImport)
            {
                _logger.LogInformation("WorkerClientesCrmImport DESACTIVADO por configuración (EnableImport=false).");
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
                return;
            }

            var hour = Math.Min(23, Math.Max(0, _settings.ImportHour));
            _logger.LogInformation("WorkerClientesCrmImport iniciado. Hora diaria de ejecución: {0:00}:00", hour);

            while (!stoppingToken.IsCancellationRequested)
            {
                var nextRun = ComputeNextRun(DateTime.Now, hour);
                var wait = nextRun - DateTime.Now;
                if (wait < TimeSpan.Zero) wait = TimeSpan.FromSeconds(1);

                _logger.LogInformation("Próxima ejecución import CRM→Sage: {0:yyyy-MM-dd HH:mm:ss} (en {1})", nextRun, wait);

                try
                {
                    await DelayLongAsync(wait, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    var ctrl = new GriesserSyncClientesImportController(
                        _apiController, _logger, _serviceScopeFactory, _settings);
                    await ctrl.ImportarAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error en ejecución del WorkerClientesCrmImport");
                }

                // Pequeña espera tras ejecutar, para asegurarnos de no encadenar
                // dos ejecuciones si el job acabó en el mismo minuto.
                try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }

            _logger.LogInformation("WorkerClientesCrmImport detenido");
        }

        /// <summary>
        /// Calcula el próximo DateTime cuya hora coincide con <paramref name="hour"/>:00:00.
        /// Si ya ha pasado hoy, devuelve la del día siguiente.
        /// </summary>
        internal static DateTime ComputeNextRun(DateTime now, int hour)
        {
            var candidate = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0, DateTimeKind.Local);
            if (candidate <= now)
                candidate = candidate.AddDays(1);
            return candidate;
        }

        /// <summary>
        /// Task.Delay tiene un máximo de aprox. 24.85 días. Para que el worker
        /// soporte futuras configuraciones (ej: ejecución semanal) lo
        /// envolvemos por si acaso.
        /// </summary>
        private static async Task DelayLongAsync(TimeSpan total, CancellationToken token)
        {
            var max = TimeSpan.FromMilliseconds(int.MaxValue);
            while (total > TimeSpan.Zero)
            {
                var chunk = total > max ? max : total;
                await Task.Delay(chunk, token);
                total -= chunk;
            }
        }

        private static ClientesCrmSyncSettings LoadSettings(IConfiguration cfg)
        {
            // Mismo loader que WorkerClientesCrm: leemos la sección compartida.
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

                if (bool.TryParse(sec["EnableImport"], out var ei)) s.EnableImport = ei;
                if (int.TryParse(sec["ImportHour"], out var ih)) s.ImportHour = ih;
            }
            return s;
        }
    }
}
