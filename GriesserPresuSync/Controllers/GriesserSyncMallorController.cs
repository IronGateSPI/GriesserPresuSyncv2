using System;
using System.Collections.Generic;
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
    /// Orquesta la sincronización de mallorquinas.
    /// Espejo de GriesserSyncPresuController:
    ///   - obtiene budgets desde la API a partir del último id sincronizado
    ///   - para cada budget, persiste cabecera + líneas + mandos (transaccional)
    /// </summary>
    public class GriesserSyncMallorController
    {
        private readonly MiGriesserMallorquinasApiController _apiController;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public GriesserSyncMallorController(
            MiGriesserMallorquinasApiController apiController,
            ILogger logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _apiController = apiController;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        async Task<MallorquinaBudget[]> GetMallorquinasAsync(int initialId = -1)
        {
            // La API puede devolver un array o un diccionario { "id": {budget} }.
            // Intentamos primero array (estilo presupuestos); si falla, diccionario.
            try
            {
                var arr = await _apiController.GetAsync<MallorquinaBudget[]>(initialId);
                if (arr != null) return arr;
            }
            catch
            {
                // continúa con diccionario
            }

            try
            {
                var dict = await _apiController.GetAsync<Dictionary<string, MallorquinaBudget>>(initialId);
                if (dict != null) return dict.Values.Where(b => b != null).ToArray();
            }
            catch (Exception e)
            {
                _logger.LogError($"No se pudo deserializar respuesta de la API: {e.Message}");
            }

            return Array.Empty<MallorquinaBudget>();
        }

        public async void syncMallorquinas()
        {
            var initialId = await GetLastSyncID();

            _logger.LogInformation("Iniciando la sincronización de mallorquinas");
            try
            {
                var budgets = await GetMallorquinasAsync(initialId);

                _logger.LogInformation($"Recuperadas {budgets.Length} mallorquinas");

                // Filtro de seguridad client-side: si la API ignora id_from,
                // descartamos los que ya tenemos para mantener idempotencia.
                if (initialId != -1)
                {
                    budgets = budgets.Where(b => b != null && b.id_budget > initialId).ToArray();
                    _logger.LogInformation($"Tras filtrado por id_from > {initialId}: {budgets.Length} nuevas");
                }

                foreach (var budget in budgets)
                {
                    try
                    {
                        await guardaMallorquina(budget);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        /// <summary>
        /// Devuelve el mayor id_budget ya sincronizado, o -1 si no hay nada.
        /// Usamos MaxAsync con proyección a int? para que EF emita
        /// SELECT MAX(id_budget) FROM IG_GriesserSyncMallorquinas
        /// y NO materialice la entidad completa: así evitamos el clásico
        /// SqlNullValueException si una columna mapeada a tipo valor no anulable
        /// contiene NULL en BD.
        /// </summary>
        private async Task<int> GetLastSyncID()
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();
                    var maxId = await dbContext.IG_GriesserSyncMallorquinas
                        .Select(l => (int?)l.id_budget)
                        .MaxAsync();

                    if (maxId.HasValue)
                    {
                        _logger.LogInformation($"El último ID de mallorquina sincronizado es: {maxId.Value}");
                        return maxId.Value;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "No se pudo obtener el último ID de mallorquina; se usará -1");
            }
            return -1;
        }

        /// <summary>
        /// Persiste un budget completo (cabecera + N líneas + N mandos) de forma idempotente.
        /// Si la cabecera ya existía no se duplica. Toda la operación va en una transacción EF
        /// (excepto cuando el provider es InMemory, usado únicamente en tests).
        /// </summary>
        private async Task guardaMallorquina(MallorquinaBudget budget)
        {
            if (budget == null)
            {
                _logger.LogWarning("Budget null recibido, se omite");
                return;
            }

            _logger.LogInformation($"Inicio de sincronización de mallorquina {budget.id_budget} - {budget.presupuesto}");

            var idLinea = $"MALLOR_{budget.id_budget}";

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();

                var supportsTx = !string.Equals(
                    dbContext.Database.ProviderName,
                    "Microsoft.EntityFrameworkCore.InMemory",
                    StringComparison.OrdinalIgnoreCase);

                Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx = null;
                if (supportsTx)
                {
                    tx = await dbContext.Database.BeginTransactionAsync();
                }

                try
                {
                    // Idempotencia con AnyAsync (EF emite EXISTS(...) y NO
                    // materializa la cabecera): así no fallamos si la fila
                    // existente tuviera NULL en alguna columna no anulable.
                    var cabExiste = await dbContext.IG_GriesserSyncMallorquinas
                        .AnyAsync(c => c.IdLinea == idLinea);

                    if (cabExiste)
                    {
                        _logger.LogInformation($"La mallorquina {idLinea} ya existe; se omite");
                        return;
                    }

                    // Cabecera
                    var cabecera = new IG_Mallorquina_Cabecera
                    {
                        IdLinea = idLinea,
                        id_budget = budget.id_budget,
                        cod_client = budget.cod_client,
                        date_created = budget.date_created,
                        presupuesto = budget.presupuesto,
                        expiration_date = budget.expiration_date,
                        id_mallorquina_color = budget.id_mallorquina_color,
                        cod_mallorquina_color = budget.cod_mallorquina_color,
                        id_mallorquina_acabado = budget.id_mallorquina_acabado,
                        cod_mallorquina_acabado = budget.cod_mallorquina_acabado,
                        num_lines = budget.num_lines,
                        price_lines = budget.price_lines,
                        num_incrementos = budget.num_incrementos,
                        price_incrementos = budget.price_incrementos,
                        price_color = budget.price_color,
                        importe_transporte = budget.importe_transporte,
                        importe_instalacion = budget.importe_instalacion,
                        total = budget.total,
                        IsSincronized = false
                    };
                    dbContext.IG_GriesserSyncMallorquinas.Add(cabecera);

                    // Líneas + mandos
                    if (budget.line_n != null)
                    {
                        int lineNumber = 1;
                        foreach (var l in budget.line_n)
                        {
                            if (l == null)
                            {
                                lineNumber++;
                                continue;
                            }

                            var linea = new IG_Mallorquina_Linea
                            {
                                IdLineaPadre = idLinea,
                                IdLinea = lineNumber,
                                pos = l.pos,
                                id_mallorquina_tipo = l.id_mallorquina_tipo,
                                cod_mallorquina_tipo = l.cod_mallorquina_tipo,
                                id_mallorquina_modelo = l.id_mallorquina_modelo,
                                cod_mallorquina_modelo = l.cod_mallorquina_modelo,
                                ancho_hueco = l.ancho_hueco,
                                alto_hueco = l.alto_hueco,
                                id_mallorquina_esquema = l.id_mallorquina_esquema,
                                cod_mallorquina_esquema = l.cod_mallorquina_esquema,
                                id_mallorquina_tipologia_instalacion = l.id_mallorquina_tipologia_instalacion,
                                cod_mallorquina_tipologia_instalacion = l.cod_mallorquina_tipologia_instalacion,
                                motorizado = l.motorizado,
                                ancho_hoja = l.ancho_hoja,
                                alto_hoja = l.alto_hoja,
                                precio_por_hoja = l.precio_por_hoja,
                                num_hojas = l.num_hojas,
                                precio_herraje = l.precio_herraje,
                                precio_incrementos = l.precio_incrementos,
                                precio_automatismos = l.precio_automatismos,
                                units = l.units,
                                total = l.total,
                                id_automatismo_marca = l.id_automatismo_marca,
                                cod_automatismo_marca = l.cod_automatismo_marca,
                                id_automatismo_receptor = l.id_automatismo_receptor,
                                cod_automatismo_receptor = l.cod_automatismo_receptor,
                                precio_receptor = l.precio_receptor,
                                precio_mandos = l.precio_mandos
                            };
                            dbContext.IG_GriesserSyncMallorquinas_Lineas.Add(linea);

                            if (l.mando_n != null)
                            {
                                foreach (var m in l.mando_n)
                                {
                                    if (m == null) continue;

                                    var mando = new IG_Mallorquina_Mando
                                    {
                                        IdLineaPadre = idLinea,
                                        IdLinea = lineNumber,
                                        id_automatismo_mando = m.id_automatismo_mando,
                                        cod_automatismo_mando = m.cod_automatismo_mando,
                                        price = m.price,
                                        units = m.units,
                                        total = m.total
                                    };
                                    dbContext.IG_GriesserSyncMallorquinas_Mandos.Add(mando);
                                }
                            }

                            lineNumber++;
                        }
                    }

                    await dbContext.SaveChangesAsync();
                    if (tx != null) await tx.CommitAsync();

                    _logger.LogInformation($"Guardada la mallorquina {idLinea} para sincronizar");
                }
                catch (Exception e)
                {
                    if (tx != null) await tx.RollbackAsync();
                    _logger.LogError($"Error guardando mallorquina {idLinea}: {e.Message}");
                    throw;
                }
                finally
                {
                    tx?.Dispose();
                }
            }
        }
    }
}