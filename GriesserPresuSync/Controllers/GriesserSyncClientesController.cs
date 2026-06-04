using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using GriesserPresuSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static GriesserPresuSync.Controllers.MiGriesserContext;

namespace GriesserPresuSync.Controllers
{
    /// <summary>
    /// Orquesta la sincronización Sage → CRM.
    /// Espejo conceptual de GriesserSyncMallorController, pero con dos giros:
    ///
    ///   - El "qué traer" no viene de una API externa, sino de la TABLA COLA
    ///     dbo.IG_CRM_ClientesPendientes (alimentada por los triggers de
    ///     Clientes y ClientesImportesRiesgo).
    ///
    ///   - Para el SELECT de datos no usamos EF (la tabla Clientes de Sage
    ///     no está mapeada y mapearla entera sería pesado y frágil): usamos
    ///     ADO.NET directo contra la misma conexión que EF nos proporciona.
    ///
    /// Patrón de robustez (mismo que el resto de controllers):
    ///   - AnyAsync para chequeos (no materializa).
    ///   - MaxAsync con proyección int? (no usado aquí pero compatible).
    ///   - Transacción EF condicionada a provider != InMemory (para tests).
    ///   - Idempotencia: si el mismo cliente tiene N filas Pendiente, se
    ///     procesa UNA llamada PUT y se marcan TODAS sus filas en bloque.
    ///   - Backoff por intentos: cuando un envío falla, sube Intentos; al
    ///     llegar al máximo, EstadoEnvio pasa a 'Error'.
    /// </summary>
    public class GriesserSyncClientesController
    {
        private readonly ClientesCrmApiController _apiController;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ClientesCrmSyncSettings _settings;

        public GriesserSyncClientesController(
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
        /// Punto de entrada del worker. Recorre la cola y, por cada
        /// CodigoCliente con filas Pendiente, manda al CRM una sola PUT
        /// y marca todas sus filas con el estado resultante.
        /// </summary>
        public async Task SyncClientesAsync()
        {
            _logger.LogInformation("Inicio ciclo Sage → CRM");

            List<string> codigos;
            try
            {
                codigos = await GetClientesPendientesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error leyendo cola de pendientes");
                return;
            }

            if (codigos == null || codigos.Count == 0)
            {
                _logger.LogInformation("Cola vacía, no hay clientes que enviar al CRM");
                return;
            }

            _logger.LogInformation($"Pendientes únicos a procesar: {codigos.Count}");

            foreach (var cod in codigos)
            {
                try
                {
                    await ProcesaClienteAsync(cod);
                }
                catch (Exception e)
                {
                    // Nunca propagar: queremos que el siguiente cliente se procese
                    _logger.LogError(e, $"Error procesando cliente {cod}");
                }
            }
        }

        // ---------------------------------------------------------------
        // Lectura de la cola
        // ---------------------------------------------------------------

        /// <summary>
        /// Devuelve la lista DISTINCT de CodigoCliente con EstadoEnvio='Pendiente'.
        /// Excluye operaciones 'D' del envío real (sólo histórico) y respeta el
        /// máximo de intentos: si todas las filas Pendiente del cliente ya
        /// superaron MaxIntentos, no se incluye y se marcará como 'Error'.
        /// </summary>
        private async Task<List<string>> GetClientesPendientesAsync()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();

                // 1) Codes pendientes con al menos una fila bajo MaxIntentos y
                //    Operacion != 'D'. Usamos proyección (no materializa la entidad).
                var codes = await db.IG_CRM_ClientesPendientes
                    .Where(p => p.EstadoEnvio == "Pendiente"
                                && p.Intentos < _settings.MaxIntentos
                                && p.Operacion != "D")
                    .Select(p => p.CodigoCliente)
                    .Distinct()
                    .ToListAsync();

                // 2) Filas 'D' pendientes: las marcamos como Descartado
                //    (no se envía nada al CRM porque acordamos no actuar en bajas).
                var bajas = await db.IG_CRM_ClientesPendientes
                    .Where(p => p.EstadoEnvio == "Pendiente" && p.Operacion == "D")
                    .ToListAsync();

                if (bajas.Count > 0)
                {
                    foreach (var b in bajas)
                    {
                        b.EstadoEnvio = "Descartado";
                        b.FechaProcesado = DateTime.Now;
                    }
                    await db.SaveChangesAsync();
                    _logger.LogInformation($"Marcadas {bajas.Count} filas 'D' como Descartado");
                }

                // 3) Filas que han superado MaxIntentos → 'Error'
                var agotadas = await db.IG_CRM_ClientesPendientes
                    .Where(p => p.EstadoEnvio == "Pendiente" && p.Intentos >= _settings.MaxIntentos)
                    .ToListAsync();
                if (agotadas.Count > 0)
                {
                    foreach (var a in agotadas)
                    {
                        a.EstadoEnvio = "Error";
                        a.FechaProcesado = DateTime.Now;
                    }
                    await db.SaveChangesAsync();
                    _logger.LogWarning($"{agotadas.Count} filas pasaron a Error por agotar reintentos");
                }

                return codes;
            }
        }

        // ---------------------------------------------------------------
        // Procesado por cliente
        // ---------------------------------------------------------------

        private async Task ProcesaClienteAsync(string codigoCliente)
        {
            ClienteCrmPayload payload;

            // 1) Consulta Sage (ADO.NET) — defensa client-side: si el cliente
            //    ha dejado de ser partner entre el trigger y ahora, devuelve null
            //    y simplemente marcamos como Descartado.
            try
            {
                payload = await ConsultaClienteAsync(codigoCliente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"No se pudo leer datos del cliente {codigoCliente}");
                await ApuntaFalloAsync(codigoCliente, $"SELECT falló: {ex.Message}");
                return;
            }

            if (payload == null)
            {
                _logger.LogInformation($"Cliente {codigoCliente} no cumple filtro (zzpartner=-1 / CLI / empresa). Se descarta.");
                await DescartaClienteAsync(codigoCliente);
                return;
            }

            // 2) Desglose de facturación últimos 5 años — query independiente.
            //    Si falla, no abortamos el envío: el CRM recibirá "{}" en ese campo
            //    y el error quedará registrado en el log para revisión.
            try
            {
                payload.FacturacionDesglosada = await ConsultaDesglosadaAsync(codigoCliente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"No se pudo leer desglose de facturación del cliente {codigoCliente}. Se enviará vacío.");
                payload.FacturacionDesglosada = new Dictionary<string, Dictionary<string, ArticuloDetalleDesglose>>();
            }

            // 3) PUT al CRM
            var result = await _apiController.PutClienteAsync(payload);

            // 4) Persistencia de resultado, transaccional
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();
                var supportsTx = !string.Equals(
                    db.Database.ProviderName,
                    "Microsoft.EntityFrameworkCore.InMemory",
                    StringComparison.OrdinalIgnoreCase);

                Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx = null;
                if (supportsTx) tx = await db.Database.BeginTransactionAsync();

                try
                {
                    // Todas las filas Pendiente de este cliente
                    var filas = await db.IG_CRM_ClientesPendientes
                        .Where(p => p.CodigoCliente == codigoCliente
                                    && p.EstadoEnvio == "Pendiente")
                        .ToListAsync();

                    foreach (var f in filas)
                    {
                        f.PayloadEnviado = result.Payload;
                        if (result.Ok)
                        {
                            f.EstadoEnvio = "Enviado";
                            f.FechaProcesado = DateTime.Now;
                            f.UltimoError = null;
                        }
                        else
                        {
                            f.Intentos = f.Intentos + 1;
                            f.UltimoError = Truncate(result.Error, 2000);
                            if (f.Intentos >= _settings.MaxIntentos)
                            {
                                f.EstadoEnvio = "Error";
                                f.FechaProcesado = DateTime.Now;
                            }
                            // Si aún no alcanzó MaxIntentos, queda como Pendiente
                            // y entrará en la próxima iteración del worker.
                        }
                    }

                    await db.SaveChangesAsync();
                    if (tx != null) await tx.CommitAsync();

                    if (result.Ok)
                        _logger.LogInformation($"Cliente {codigoCliente} enviado al CRM (filas: {filas.Count})");
                    else
                        _logger.LogWarning($"Cliente {codigoCliente} FALLÓ — {result.Error}");
                }
                catch (Exception ex)
                {
                    if (tx != null) await tx.RollbackAsync();
                    _logger.LogError(ex, $"Error guardando estado para cliente {codigoCliente}");
                }
                finally
                {
                    tx?.Dispose();
                }
            }
        }

        // ---------------------------------------------------------------
        // Consulta ADO.NET — la query que dio el cliente filtrada por cod
        // ---------------------------------------------------------------

        /// <summary>
        /// Lanza la consulta enriquecida (Clientes + outer apply
        /// ResumenCliente + ClientesImportesRiesgo) para UN cliente.
        /// Devuelve null si no cumple los filtros (no es partner, no es CLI,
        /// no es de la empresa configurada, no existe).
        /// </summary>
        private async Task<ClienteCrmPayload> ConsultaClienteAsync(string codigoCliente)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();
                var conn = db.Database.GetDbConnection();

                // Si EF no la ha abierto aún, la abrimos nosotros.
                var huboQueAbrir = conn.State != ConnectionState.Open;
                if (huboQueAbrir) await conn.OpenAsync();

                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = SqlConsultaCliente;
                        cmd.CommandType = CommandType.Text;

                        AddParam(cmd, "@empresa", DbType.Int16, _settings.CodigoEmpresa);
                        AddParam(cmd, "@codigo", DbType.String, codigoCliente);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync()) return null;

                            return new ClienteCrmPayload
                            {
                                CodigoCliente = codigoCliente,
                                Nombre = SafeStr(reader, "nombre"),
                                Nif = SafeStr(reader, "nif"),
                                TipoVia = SafeStr(reader, "tipovia"),
                                Direccion = NormalizaDireccion(SafeStr(reader, "direccion")),
                                CodigoPostal = SafeStr(reader, "codigopostal"),
                                Municipio = SafeStr(reader, "municipio"),
                                Provincia = SafeStr(reader, "provincia"),
                                FacturacionAnual = SafeDec(reader, "baseanual"),
                                Descubierto = SafeDec(reader, "descubierto"),
                                CyC = SafeDec(reader, "cyc"),
                                CyCDescubierto = SafeDec(reader, "cycdescubierto"),
                                Descuento = SafeDec(reader, "descuento")
                            };
                        }
                    }
                }
                finally
                {
                    if (huboQueAbrir) conn.Close();
                }
            }
        }

        /// <summary>
        /// Misma consulta que pasó el cliente, parametrizada por @empresa y @codigo.
        /// Se mantiene el filtro zzpartner=-1 para que un cliente despromocionado
        /// devuelva 0 filas y la sincronización pase a 'Descartado' automáticamente.
        /// </summary>
        private const string SqlConsultaCliente = @"
SELECT
    cli.RazonSocial AS nombre,
    cli.cifdni      AS nif,
    cli.CodigoSigla AS tipovia,
    ISNULL(cli.ViaPublica, '') + ' '
        + ISNULL(cli.Numero1, '')  + ' '
        + ISNULL(cli.Numero2, '')  + ' '
        + ISNULL(cli.Escalera, '') + ' '
        + ISNULL(cli.Piso, '')     + ' '
        + ISNULL(cli.Puerta, '')   + ' '
        + ISNULL(cli.Letra, '')                          AS direccion,
    cli.CodigoPostal                                     AS codigopostal,
    cli.Municipio                                        AS municipio,
    cli.Provincia                                        AS provincia,
    fac.Baseanual                                        AS baseanual,
    r.riesgo                                             AS descubierto,
    cli.RiesgoMaximo                                     AS cyc,
    (cli.RiesgoMaximo - ISNULL(r.riesgo, 0))             AS cycdescubierto,
    cli.[%Descuento]                                     AS descuento
FROM Clientes cli
OUTER APPLY (
    SELECT SUM(Baseimponible) AS Baseanual
    FROM   ResumenCliente
    WHERE  codigoempresa     = @empresa
      AND  codigocliente     = cli.codigocliente
      AND  EjercicioFactura  = YEAR(GETDATE()) - 1
) fac
OUTER APPLY (
    SELECT SUM(ImportePendienteDoc) AS riesgo
    FROM   ClientesImportesRiesgo
    WHERE  codigoempresa = @empresa
      AND  CodigoCliente = cli.CodigoCliente
) r
WHERE cli.CodigoEmpresa            = @empresa
  AND cli.CodigoCategoriaCliente_  = 'CLI'
  AND cli.zzpartner                = -1
  AND cli.CodigoCliente            = @codigo;
";

        // ---------------------------------------------------------------
        // Consulta desglose — ADO.NET igual que ConsultaClienteAsync
        // ---------------------------------------------------------------

        /// <summary>
        /// Devuelve la facturación desglosada por ejercicio-mes y artículo
        /// de los últimos 5 años para el cliente indicado.
        /// Estructura resultado: { "2025-01": { "METV": 1234, ... }, ... }
        /// Si no hay datos devuelve diccionario vacío (nunca null).
        /// DescripcionArticulo se trae de Sage pero no se envía al CRM
        /// (el formato de la API solo admite código → unidades).
        /// </summary>
        private async Task<Dictionary<string, Dictionary<string, ArticuloDetalleDesglose>>>
            ConsultaDesglosadaAsync(string codigoCliente)
        {
            var resultado = new Dictionary<string, Dictionary<string, ArticuloDetalleDesglose>>();

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();
                var conn = db.Database.GetDbConnection();

                var huboQueAbrir = conn.State != System.Data.ConnectionState.Open;
                if (huboQueAbrir) await conn.OpenAsync();

                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = SqlConsultaDesglose;
                        cmd.CommandType = System.Data.CommandType.Text;

                        AddParam(cmd, "@empresa", System.Data.DbType.Int16, _settings.CodigoEmpresa);
                        AddParam(cmd, "@codigo", System.Data.DbType.String, codigoCliente);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var ejercicio = Convert.ToInt32(reader["ejercicio"]);
                                var mes = Convert.ToInt32(reader["mes"]);
                                var articulo = SafeStr(reader, "codarticulo");

                                var baseimponible = SafeDec(reader, "baseimponible") ?? 0m;
                                var unidades = SafeDec(reader, "unidades") ?? 0m;
                                var color = SafeStr(reader, "color");

                                if (string.IsNullOrWhiteSpace(articulo)) continue;

                                var clave = $"{ejercicio}-{mes:D2}";
                                if (!resultado.TryGetValue(clave, out var mesDict))
                                {
                                    mesDict = new Dictionary<string, ArticuloDetalleDesglose>();
                                    resultado[clave] = mesDict;
                                }
                                // Si el mismo artículo aparece más de una vez en el mes
                                // (distintas cabeceras con distinto zcolor), acumulamos
                                // importe y unidades y conservamos el primer color encontrado.
                                if (mesDict.TryGetValue(articulo, out var acum))
                                {
                                    mesDict[articulo] = new ArticuloDetalleDesglose
                                    {
                                        Importe = acum.Importe + baseimponible,
                                        Unidades = acum.Unidades + unidades,
                                        Color = acum.Color ?? color
                                    };
                                }
                                else
                                {
                                    mesDict[articulo] = new ArticuloDetalleDesglose
                                    {
                                        Importe = baseimponible,
                                        Unidades = unidades,
                                        Color = color
                                    };
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (huboQueAbrir) conn.Close();
                }
            }

            return resultado;
        }

        /// <summary>
        /// Albaranes facturados (NumeroFactura != 0) de los últimos 5 años,
        /// agrupados por ejercicio, mes, artículo y cliente.
        /// Columnas devueltas: ejercicio, mes, codarticulo, unidades.
        /// (DescripcionArticulo también se trae de Sage por si se necesita
        /// en el futuro, pero el controller no la incluye en el payload.)
        /// </summary>
        private const string SqlConsultaDesglose = @"
SELECT
    lin.EjercicioAlbaran                    AS ejercicio,
    MONTH(lin.FechaAlbaran)                 AS mes,
    lin.CodigoArticulo                      AS codarticulo,
    art.DescripcionArticulo                 AS descripcion,
    cab.zcolor                              AS color,
    SUM(lin.Unidades2_)                     AS unidades,
    SUM(lin.Baseimponible)                  AS baseimponible
						   
FROM LineasAlbaranCliente lin
LEFT JOIN CabeceraAlbaranCliente cab
    ON  cab.CodigoEmpresa    = lin.CodigoEmpresa
    AND cab.EjercicioAlbaran = lin.EjercicioAlbaran
    AND cab.SerieAlbaran     = lin.SerieAlbaran
    AND cab.NumeroAlbaran    = lin.NumeroAlbaran
LEFT JOIN Articulos art
    ON  art.CodigoEmpresa  = lin.CodigoEmpresa
    AND art.CodigoArticulo = lin.CodigoArticulo
WHERE lin.CodigoEmpresa   = @empresa
  AND cab.CodigoCliente   = @codigo
  AND lin.NumeroFactura   <> 0
  AND lin.FechaAlbaran    > DATEADD(YEAR, -5, GETDATE())
  AND lin.CodigoFamilia  IN ('1Ma','1P','1PE','1PG','1T','1To','1We','AU','S')
GROUP BY
    lin.EjercicioAlbaran,
    MONTH(lin.FechaAlbaran),
    lin.CodigoArticulo,
    art.DescripcionArticulo,
    cab.zcolor
ORDER BY
    lin.EjercicioAlbaran,
    MONTH(lin.FechaAlbaran);
";

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private async Task ApuntaFalloAsync(string codigoCliente, string error)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();
                var filas = await db.IG_CRM_ClientesPendientes
                    .Where(p => p.CodigoCliente == codigoCliente
                                && p.EstadoEnvio == "Pendiente")
                    .ToListAsync();
                foreach (var f in filas)
                {
                    f.Intentos++;
                    f.UltimoError = Truncate(error, 2000);
                    if (f.Intentos >= _settings.MaxIntentos)
                    {
                        f.EstadoEnvio = "Error";
                        f.FechaProcesado = DateTime.Now;
                    }
                }
                await db.SaveChangesAsync();
            }
        }

        private async Task DescartaClienteAsync(string codigoCliente)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();
                var filas = await db.IG_CRM_ClientesPendientes
                    .Where(p => p.CodigoCliente == codigoCliente
                                && p.EstadoEnvio == "Pendiente")
                    .ToListAsync();
                foreach (var f in filas)
                {
                    f.EstadoEnvio = "Descartado";
                    f.FechaProcesado = DateTime.Now;
                }
                await db.SaveChangesAsync();
            }
        }

        private static void AddParam(DbCommand cmd, string name, DbType type, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.DbType = type;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        private static string SafeStr(IDataReader r, string col)
        {
            var i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? null : r.GetValue(i)?.ToString();
        }

        private static decimal? SafeDec(IDataReader r, string col)
        {
            var i = r.GetOrdinal(col);
            if (r.IsDBNull(i)) return null;
            var raw = r.GetValue(i);
            try { return Convert.ToDecimal(raw); }
            catch { return null; }
        }

        /// <summary>
        /// La concatenación de la dirección puede quedar con muchos espacios
        /// si hay campos vacíos (ej: "CALLE MAYOR   12   "). Colapsamos
        /// espacios múltiples y trimamos.
        /// </summary>
        private static string NormalizaDireccion(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return System.Text.RegularExpressions.Regex
                .Replace(raw.Trim(), "\\s+", " ");
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
