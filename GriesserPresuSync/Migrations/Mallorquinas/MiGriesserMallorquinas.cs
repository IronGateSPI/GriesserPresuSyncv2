using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GriesserPresuSync.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using static GriesserPresuSync.Controllers.MiGriesserMallorquinas;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using MiGriesserMallorquinas.Models;
using MiGriesserMallorquinas.Services;
using Microsoft.Data.SqlClient;


namespace MiGriesserMallorquinas
{
    public class BudgetSyncProcess
    {
        private readonly string _connectionString;
        //private readonly BudgetService _budgetService;
        private readonly string _apiUrl;

        public BudgetSyncProcess(string connectionString, string apiUrl)
        {
            _connectionString = connectionString;
            _apiUrl = apiUrl;
        }

        /// <summary>
        /// Proceso principal: Obtiene budgets de la API e inserta los que no existen en BD
        /// </summary>
        public async Task SyncBudgetsAsync(int daysBack = 30)
        {
            var budgetService = new BudgetService(_apiUrl);
            try
            {
                Console.WriteLine("=== SINCRONIZACIÓN DE BUDGETS ===\n");

                // 1. Obtener budgets existentes en BD
                var existingBudgets = await GetExistingBudgetsFromDBAsync();
                Console.WriteLine($"Budgets existentes en BD: {existingBudgets.Count}\n");

                // 2. Obtener budgets desde la API
                var dateFrom = DateTime.Now.AddDays(-daysBack);
                var apiResponse = await budgetService.GetBudgetsFromDateAsync(dateFrom);
                Console.WriteLine($"Budgets obtenidos de la API: {apiResponse.Count}\n");

                // 3. Filtrar budgets que NO existen
                var budgetsToInsert = apiResponse.Values
                    .Where(b => b != null && !existingBudgets.Contains(b.IdBudget))
                    .ToList();

                Console.WriteLine($"Budgets nuevos a insertar: {budgetsToInsert.Count}\n");

                if (budgetsToInsert.Count == 0)
                {
                    Console.WriteLine("No hay budgets nuevos para insertar.");
                    return;
                }

                // 4. Insertar cada budget nuevo
                int insertedCount = 0;
                int errorCount = 0;

                foreach (var budget in budgetsToInsert)
                {
                    Console.WriteLine($"Insertando budget {budget.IdBudget}...");

                    bool success = await InsertBudgetAsync(budget);

                    if (success)
                    {
                        insertedCount++;
                        Console.WriteLine($"  ✓ Budget {budget.IdBudget} insertado\n");
                    }
                    else
                    {
                        errorCount++;
                        Console.WriteLine($"  ✗ Error insertando budget {budget.IdBudget}\n");
                    }
                }

                // 5. Resumen
                Console.WriteLine("\n=== RESUMEN ===");
                Console.WriteLine($"Insertados correctamente: {insertedCount}");
                Console.WriteLine($"Errores: {errorCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en sincronización: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todos los id_budget que ya existen en la BD
        /// </summary>
        private async Task<HashSet<int>> GetExistingBudgetsFromDBAsync()
        {
            var existingIds = new HashSet<int>();

            const string query = @"
                SELECT DISTINCT id_budget 
                FROM IG_GriesserSyncMallor 
                WHERE id_budget IS NOT NULL";

            //using (var connection = new configuration(_connectionString))
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        existingIds.Add(reader.GetInt32(0));
                    }
                }
            }

            return existingIds;
        }

        /// <summary>
        /// Inserta un budget completo (cabecera + líneas + mandos)
        /// </summary>
        private async Task<bool> InsertBudgetAsync(BudgetResponse budget)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Generar IdLinea único
                        string idLinea = $"BUDGET_{budget.IdBudget}_{DateTime.Now:yyyyMMddHHmmss}";

                        // 1. Insertar cabecera
                        await InsertBudgetHeaderAsync(budget, idLinea, connection, transaction);

                        // 2. Insertar líneas
                        if (budget.Lines != null && budget.Lines.Count > 0)
                        {
                            int lineNumber = 1;
                            foreach (var line in budget.Lines)
                            {
                                await InsertBudgetLineAsync(idLinea, lineNumber, line, connection, transaction);

                                // 3. Insertar mandos de esta línea
                                if (line.Mandos != null && line.Mandos.Count > 0)
                                {
                                    foreach (var mando in line.Mandos)
                                    {
                                        //if (mando.IdAutomatismoMando)
                                        //{
                                            await InsertMandoAsync(idLinea, lineNumber, mando, connection, transaction);
                                        //}
                                    }
                                }

                                lineNumber++;
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"  Error: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Inserta la cabecera del budget en IG_GriesserSyncMallor2
        /// </summary>
        private async Task InsertBudgetHeaderAsync(BudgetResponse budget, string idLinea,
            SqlConnection connection, SqlTransaction transaction)
        {
            const string sql = @"
                INSERT INTO IG_GriesserSyncMallor (
                    IdLinea, id_budget, cod_client, date_created, presupuesto, expiration_date,
                    id_mallorquina_color, cod_mallorquina_color, id_mallorquina_acabado, 
                    cod_mallorquina_acabado, num_lines, price_lines, num_incrementos, 
                    price_incrementos, price_color, importe_transporte, importe_instalacion, total
                ) VALUES (
                    @IdLinea, @IdBudget, @CodClient, @DateCreated, @Presupuesto, @ExpirationDate,
                    @IdMallorquinaColor, @CodMallorquinaColor, @IdMallorquinaAcabado,
                    @CodMallorquinaAcabado, @NumLines, @PriceLines, @NumIncrementos,
                    @PriceIncrementos, @PriceColor, @ImporteTransporte, @ImporteInstalacion, @Total
                )";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@IdLinea", idLinea);
                command.Parameters.AddWithValue("@IdBudget", budget.IdBudget);
                command.Parameters.AddWithValue("@CodClient", budget.CodClient ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DateCreated",!string.IsNullOrEmpty(budget.DateCreated) ?DateTime.Parse(budget.DateCreated).ToString("yyyyMMdd HH:mm:ss") :(object)DBNull.Value);
                command.Parameters.AddWithValue("@Presupuesto", budget.Presupuesto ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ExpirationDate",!string.IsNullOrEmpty(budget.ExpirationDate) ?DateTime.Parse(budget.ExpirationDate).ToString("yyyyMMdd HH:mm:ss") :(object)DBNull.Value);
                command.Parameters.AddWithValue("@IdMallorquinaColor", budget.IdMallorquinaColor ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CodMallorquinaColor", budget.CodMallorquinaColor ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IdMallorquinaAcabado", budget.IdMallorquinaAcabado ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CodMallorquinaAcabado", budget.CodMallorquinaAcabado ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@NumLines", budget.NumLines ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PriceLines", budget.PriceLines ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@NumIncrementos", budget.NumIncrementos ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PriceIncrementos", budget.PriceIncrementos ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PriceColor", budget.PriceColor ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ImporteTransporte", budget.ImporteTransporte ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ImporteInstalacion", budget.ImporteInstalacion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Total", budget.Total ?? (object)DBNull.Value);

                /*var debugSql = sql;
                foreach (SqlParameter param in command.Parameters)
                {
                    var value = param.Value == DBNull.Value ? "NULL" :
                                param.Value is string ? $"'{param.Value}'" :
                                param.Value?.ToString() ?? "NULL";
                    debugSql = debugSql.Replace(param.ParameterName, value);
                }
                Console.WriteLine("=== SQL A EJECUTAR ===");
                Console.WriteLine(debugSql);
                Console.WriteLine("=====================");*/

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserta una línea del budget en IG_GriesserSyncMallor2_Lineas
        /// </summary>
        private async Task InsertBudgetLineAsync(string idLineaPadre, int lineNumber,
            BudgetLineResponse line, SqlConnection connection, SqlTransaction transaction)
        {
            const string sql = @"
                INSERT INTO IG_GriesserSyncMallor_Lineas (
                    IdLineaPadre, IdLinea, pos, id_mallorquina_tipo, cod_mallorquina_tipo,
                    id_mallorquina_modelo, cod_mallorquina_modelo, ancho_hueco, alto_hueco,
                    id_mallorquina_esquema, cod_mallorquina_esquema, id_mallorquina_tipologia_instalacion,
                    cod_mallorquina_tipologia_instalacion, motorizado, ancho_hoja, alto_hoja,
                    precio_por_hoja, num_hojas, precio_herraje, precio_incrementos,
                    precio_automatismos, units, total, id_automatismo_marca, cod_automatismo_marca,
                    id_automatismo_receptor, cod_automatismo_receptor, precio_receptor, precio_mandos
                ) VALUES (
                    @IdLineaPadre, @IdLinea, @Pos, @IdMallorquinaTipo, @CodMallorquinaTipo,
                    @IdMallorquinaModelo, @CodMallorquinaModelo, @AnchoHueco, @AltoHueco,
                    @IdMallorquinaEsquema, @CodMallorquinaEsquema, @IdMallorquinaTipologiaInstalacion,
                    @CodMallorquinaTipologiaInstalacion, @Motorizado, @AnchoHoja, @AltoHoja,
                    @PrecioPorHoja, @NumHojas, @PrecioHerraje, @PrecioIncrementos,
                    @PrecioAutomatismos, @Units, @Total, @IdAutomatismoMarca, @CodAutomatismoMarca,
                    @IdAutomatismoReceptor, @CodAutomatismoReceptor, @PrecioReceptor, @PrecioMandos
                )";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@IdLineaPadre", idLineaPadre);
                command.Parameters.AddWithValue("@IdLinea", lineNumber);
                command.Parameters.AddWithValue("@Pos", line.Pos ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IdMallorquinaTipo", line.IdMallorquinaTipo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CodMallorquinaTipo", line.CodMallorquinaTipo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IdMallorquinaModelo", line.IdMallorquinaModelo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CodMallorquinaModelo", line.CodMallorquinaModelo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AnchoHueco", line.AnchoHueco ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AltoHueco", line.AltoHueco ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IdMallorquinaEsquema", line.IdMallorquinaEsquema ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CodMallorquinaEsquema", line.CodMallorquinaEsquema ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IdMallorquinaTipologiaInstalacion", line.IdMallorquinaTipologiaInstalacion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CodMallorquinaTipologiaInstalacion", line.CodMallorquinaTipologiaInstalacion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Motorizado", line.Motorizado ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AnchoHoja", line.AnchoHoja ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AltoHoja", line.AltoHoja ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PrecioPorHoja", line.PrecioPorHoja ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@NumHojas", line.NumHojas ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PrecioHerraje", line.PrecioHerraje ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PrecioIncrementos", line.PrecioIncrementos ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PrecioAutomatismos", line.PrecioAutomatismos ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Units", line.Units ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Total", line.Total ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IdAutomatismoMarca", line.IdAutomatismoMarca ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CodAutomatismoMarca", line.CodAutomatismoMarca ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IdAutomatismoReceptor", line.IdAutomatismoReceptor ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CodAutomatismoReceptor", line.CodAutomatismoReceptor ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PrecioReceptor", line.PrecioReceptor ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PrecioMandos", line.PrecioMandos ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Inserta un mando en IG_GriesserSyncMallor2_Mandos
        /// </summary>
        private async Task InsertMandoAsync(string idLineaPadre, int idLinea,
            MandoResponse mando, SqlConnection connection, SqlTransaction transaction)
        {
            const string sql = @"
                INSERT INTO IG_GriesserSyncMallor_Mandos (
                    IdLineaPadre, IdLinea, id_automatismo_mando, 
                    cod_automatismo_mando, price, units, total
                ) VALUES (
                    @IdLineaPadre, @IdLinea, @IdAutomatismoMando,
                    @CodAutomatismoMando, @Price, @Units, @Total
                )";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@IdLineaPadre", idLineaPadre);
                command.Parameters.AddWithValue("@IdLinea", idLinea);
                command.Parameters.AddWithValue("@IdAutomatismoMando", mando.IdAutomatismoMando);
                command.Parameters.AddWithValue("@CodAutomatismoMando", mando.CodAutomatismoMando ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Price", mando.Price);
                command.Parameters.AddWithValue("@Units", mando.Units ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Total", mando.Total);

                await command.ExecuteNonQueryAsync();
            }
        }

        // Métodos auxiliares de conversión
        private DateTime? ParseDate(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            if (DateTime.TryParse(input, null, DateTimeStyles.RoundtripKind, out var result))
            {
                // Validar que esté dentro del rango de SQL Server DateTime
                var sqlMinDate = new DateTime(1753, 1, 1);
                var sqlMaxDate = new DateTime(9999, 12, 31, 23, 59, 59);

                if (result >= sqlMinDate && result <= sqlMaxDate)
                    return result;
            }

            return null; // Devolver null en lugar de DateTime.MinValue
        }
    }
}