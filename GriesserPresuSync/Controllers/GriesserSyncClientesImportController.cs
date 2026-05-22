using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using GriesserPresuSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GriesserPresuSync.Controllers
{
    /// <summary>
    /// Orquesta la bajada CRM → Sage (job nocturno).
    ///
    /// Flujo:
    ///   1) GET {ApiUrl}/all/sync_sage_distributor
    ///   2) Por cada item, UPDATE en dbo.Clientes (cliente partner):
    ///        zzTarget       = target_de_ventas
    ///        zzTipoCliente  = partner_type
    ///      Solo si el CodigoCliente existe Y es partner (zzpartner = -1).
    ///   3) Loguea contadores: actualizados / no_encontrados / errores.
    ///
    /// Diseño:
    ///   - Usamos ADO.NET (no EF) porque Clientes no está mapeada y porque
    ///     hacer UPDATEs ad-hoc directos a Sage es más limpio que mapear la
    ///     tabla entera por dos campos.
    ///   - Cada UPDATE va aislado: si uno falla no aborta el resto.
    ///   - Idempotente: ejecutar dos veces seguidas deja los mismos valores.
    /// </summary>
    public class GriesserSyncClientesImportController
    {
        private readonly ClientesCrmApiController _apiController;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ClientesCrmSyncSettings _settings;

        public GriesserSyncClientesImportController(
            ClientesCrmApiController apiController,
            ILogger logger,
            IServiceScopeFactory serviceScopeFactory,
            ClientesCrmSyncSettings settings)
        {
            _apiController = apiController;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _settings = settings ?? new ClientesCrmSyncSettings();
        }

        /// <summary>
        /// Punto de entrada del job nocturno.
        /// </summary>
        public async Task ImportarAsync()
        {
            _logger.LogInformation("Inicio import CRM → Sage");

            var items = await _apiController.GetAllAsync();
            if (items == null || items.Count == 0)
            {
                _logger.LogWarning("CRM devolvió 0 items en /all/sync_sage_distributor");
                return;
            }

            _logger.LogInformation($"CRM devolvió {items.Count} partners");

            int ok = 0, noEncontrados = 0, errores = 0;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();
                var conn = db.Database.GetDbConnection();
                var huboQueAbrir = conn.State != ConnectionState.Open;
                if (huboQueAbrir) await conn.OpenAsync();

                try
                {
                    foreach (var it in items)
                    {
                        if (it == null || string.IsNullOrWhiteSpace(it.CodSage))
                            continue;

                        try
                        {
                            var n = await ActualizarClienteAsync(conn, it);
                            if (n > 0) ok++;
                            else noEncontrados++;
                        }
                        catch (Exception ex)
                        {
                            errores++;
                            _logger.LogError(ex,
                                $"Error al actualizar partner {it.CodSage}");
                        }
                    }
                }
                finally
                {
                    if (huboQueAbrir) conn.Close();
                }
            }

            _logger.LogInformation(
                $"Import CRM→Sage terminado. Actualizados: {ok}, no encontrados: {noEncontrados}, errores: {errores}");
        }

        private async Task<int> ActualizarClienteAsync(DbConnection conn, ClienteCrmImportItem it)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = SqlUpdate;
                cmd.CommandType = CommandType.Text;

                AddParam(cmd, "@empresa", DbType.Int16,  _settings.CodigoEmpresa);
                AddParam(cmd, "@cod",     DbType.String, it.CodSage);
                AddParam(cmd, "@target",  DbType.Decimal, (object)it.TargetDeVentas ?? DBNull.Value);
                AddParam(cmd, "@tipo",    DbType.String, (object)it.PartnerType    ?? DBNull.Value);

                var affected = await cmd.ExecuteNonQueryAsync();
                return affected;
            }
        }

        /// <summary>
        /// UPDATE filtrado a partners (zzpartner = -1) para no escribir en
        /// clientes que ya no lo son. Si el CRM nos manda un partner que
        /// dejó de serlo, la fila no se toca: affected = 0 y queda como
        /// "no encontrado" en los contadores.
        /// </summary>
        private const string SqlUpdate = @"
UPDATE dbo.Clientes
   SET zzTarget      = @target,
       zzTipoCliente = @tipo
 WHERE CodigoEmpresa            = @empresa
   AND CodigoCliente            = @cod
   AND CodigoCategoriaCliente_  = 'CLI'
   AND zzpartner                = -1;
";

        private static void AddParam(DbCommand cmd, string name, DbType type, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.DbType = type;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}
