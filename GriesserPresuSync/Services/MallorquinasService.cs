using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using GriesserPresuSync.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static GriesserPresuSync.Controllers.MiGriesserMallorquinas;

namespace GriesserPresuSync.Services
{
    public class MallorquinasService
    {
        private readonly HttpClient _httpClient;
        private readonly MiGriesserMallorquinasContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MallorquinasService> _logger;

        public MallorquinasService(
            HttpClient httpClient,
            MiGriesserMallorquinasContext context,
            IConfiguration configuration,
            ILogger<MallorquinasService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SyncMallorquinasAsync(DateTime? fromDate = null)
        {
            try
            {
                var dateParam = fromDate?.ToString("yyyy-MM-dd") ?? "2025-01-01";
                var apiUrl = $"https://www.migriesser.com/es/mallorquinas_budgets/api?date_from={dateParam}&details=true";

                _logger.LogInformation($"Calling API: {apiUrl}");

                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"API response received: {jsonContent.Length} characters");

                // Deserializar como JsonElement
                var jsonData = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                if (jsonData.ValueKind == JsonValueKind.Null || jsonData.ValueKind == JsonValueKind.Undefined)
                {
                    _logger.LogInformation("No data received from API");
                    return;
                }

                var processedBudgets = 0;

                // Recorrer cada budget en el JSON (es un diccionario con claves numéricas)
                foreach (var property in jsonData.EnumerateObject())
                {
                    var budgetId = property.Name;  // La clave (ej: "1029", "1031")
                    var budgetData = property.Value; // Los datos del presupuesto

                    try
                    {
                        await ProcessBudgetFromJsonAsync(budgetId, budgetData);
                        processedBudgets++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing budget {budgetId}");
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Sync completed successfully. Processed {processedBudgets} budgets");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during mallorquinas sync");
                throw;
            }
        }

        private async Task ProcessBudgetFromJsonAsync(string budgetId, JsonElement budgetData)
        {
            // Extraer y validar id_budget
            if (!budgetData.TryGetProperty("id_budget", out var idBudgetElement) ||
                idBudgetElement.ValueKind == JsonValueKind.Null)
            {
                _logger.LogWarning($"Budget {budgetId} without valid id_budget, skipping");
                return;
            }

            var idBudget = idBudgetElement.GetInt32();
            var idLinea = $"MALLOR_{idBudget}";

            // Verificar si ya existe
            var existingBudget = await _context.IG_GriesserSyncMallor2
                .FirstOrDefaultAsync(x => x.IdLinea == idLinea);

            if (existingBudget != null)
            {
                _context.IG_GriesserSyncMallor2.Remove(existingBudget);
            }

            // Crear nuevo presupuesto extrayendo datos del JSON
            var newBudget = new IG_GriesserSyncMallor2
            {
                IdLinea = idLinea,
                id_budget = idBudget,
                cod_client = GetStringValue(budgetData, "cod_client"),
                date_created = ParseDateTime(GetStringValue(budgetData, "date_created")),
                presupuesto = GetStringValue(budgetData, "presupuesto"),
                expiration_date = ParseDateTime(GetStringValue(budgetData, "expiration_date")),
                id_mallorquina_color = GetIntValue(budgetData, "id_mallorquina_color"),
                cod_mallorquina_color = GetStringValue(budgetData, "cod_mallorquina_color"),
                id_mallorquina_acabado = GetIntValue(budgetData, "id_mallorquina_acabado"),
                cod_mallorquina_acabado = GetStringValue(budgetData, "cod_mallorquina_acabado"),
                num_lines = GetIntValue(budgetData, "num_lines"),
                price_lines = ParseDecimal(GetStringValue(budgetData, "price_lines")),
                num_incrementos = GetIntValue(budgetData, "num_incrementos"),
                price_incrementos = ParseDecimal(GetStringValue(budgetData, "price_incrementos")),
                price_color = ParseDecimal(GetStringValue(budgetData, "price_color")),
                importe_transporte = ParseDecimal(GetStringValue(budgetData, "importe_transporte")),
                importe_instalacion = ParseDecimal(GetStringValue(budgetData, "importe_instalacion")),
                total = ParseDecimal(GetStringValue(budgetData, "total"))
            };

            _context.IG_GriesserSyncMallor2.Add(newBudget);

            // Procesar líneas si existen
            if (budgetData.TryGetProperty("line_n", out var linesElement) && linesElement.ValueKind == JsonValueKind.Array)
            {
                var lineIndex = 0;
                foreach (var lineElement in linesElement.EnumerateArray())
                {
                    await ProcessLineFromJsonAsync(idLinea, lineIndex, lineElement);
                    lineIndex++;
                }
            }
        }

        private async Task ProcessLineFromJsonAsync(string idLineaPadre, int lineIndex, JsonElement lineData)
        {
            var newLine = new IG_GriesserSyncMallor2_Lineas
            {
                IdLineaPadre = idLineaPadre,
                IdLinea = lineIndex,
                pos = GetStringValue(lineData, "pos"),
                id_mallorquina_tipo = GetIntValue(lineData, "id_mallorquina_tipo"),
                cod_mallorquina_tipo = GetStringValue(lineData, "cod_mallorquina_tipo"),
                id_mallorquina_modelo = GetIntValue(lineData, "id_mallorquina_modelo"),
                cod_mallorquina_modelo = GetStringValue(lineData, "cod_mallorquina_modelo"),
                ancho_hueco = GetIntValue(lineData, "ancho_hueco"),
                alto_hueco = GetIntValue(lineData, "alto_hueco"),
                id_mallorquina_esquema = GetIntValue(lineData, "id_mallorquina_esquema"),
                cod_mallorquina_esquema = GetStringValue(lineData, "cod_mallorquina_esquema"),
                id_mallorquina_tipologia_instalacion = GetIntValue(lineData, "id_mallorquina_tipologia_instalacion"),
                cod_mallorquina_tipologia_instalacion = GetStringValue(lineData, "cod_mallorquina_tipologia_instalacion"),
                motorizado = GetBoolValue(lineData, "motorizado"),
                ancho_hoja = GetIntValue(lineData, "ancho_hoja"),
                alto_hoja = GetIntValue(lineData, "alto_hoja"),
                precio_por_hoja = ParseDecimal(GetStringValue(lineData, "precio_por_hoja")),
                num_hojas = ParseInt(GetStringValue(lineData, "num_hojas")),
                precio_herraje = ParseDecimal(GetStringValue(lineData, "precio_herraje")),
                precio_incrementos = ParseDecimal(GetStringValue(lineData, "precio_incrementos")),
                precio_automatismos = ParseDecimal(GetStringValue(lineData, "precio_automatismos")),
                units = ParseInt(GetStringValue(lineData, "units")),
                total = ParseDecimal(GetStringValue(lineData, "total")),
                id_automatismo_marca = GetIntValue(lineData, "id_automatismo_marca"),
                cod_automatismo_marca = GetStringValue(lineData, "cod_automatismo_marca"),
                id_automatismo_receptor = GetIntValue(lineData, "id_automatismo_receptor"),
                cod_automatismo_receptor = GetStringValue(lineData, "cod_automatismo_receptor"),
                precio_receptor = ParseDecimal(GetStringValue(lineData, "precio_receptor")),
                precio_mandos = ParseDecimal(GetStringValue(lineData, "precio_mandos"))
            };

            _context.IG_GriesserSyncMallor2_Lineas.Add(newLine);

            // Procesar mandos si existen
            if (lineData.TryGetProperty("mando_n", out var mandosElement) && mandosElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var mandoElement in mandosElement.EnumerateArray())
                {
                    await ProcessMandoFromJsonAsync(idLineaPadre, lineIndex, mandoElement);
                }
            }
        }

        private async Task ProcessMandoFromJsonAsync(string idLineaPadre, int idLinea, JsonElement mandoData)
        {
            var idMando = GetIntValue(mandoData, "id_automatismo_mando");
            if (!idMando.HasValue) return;

            var newMando = new IG_GriesserSyncMallor2_Mandos
            {
                IdLineaPadre = idLineaPadre,
                IdLinea = idLinea,
                id_automatismo_mando = idMando.Value,
                cod_automatismo_mando = GetStringValue(mandoData, "cod_automatismo_mando"),
                price = ParseDecimal(GetStringValue(mandoData, "price")),
                units = GetIntValue(mandoData, "units"),
                total = ParseDecimal(GetStringValue(mandoData, "total"))
            };

            _context.IG_GriesserSyncMallor2_Mandos.Add(newMando);
        }

        // Métodos auxiliares para extraer valores del JSON de forma segura
        private string GetStringValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                if (property.ValueKind == JsonValueKind.String)
                    return property.GetString();
                if (property.ValueKind != JsonValueKind.Null)
                    return property.ToString();
            }
            return null;
        }

        private int? GetIntValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind != JsonValueKind.Null)
            {
                if (property.ValueKind == JsonValueKind.Number)
                    return property.GetInt32();
                if (property.ValueKind == JsonValueKind.String)
                {
                    var stringValue = property.GetString();
                    if (!string.IsNullOrEmpty(stringValue) && int.TryParse(stringValue, out var result))
                        return result;
                }
            }
            return null;
        }

        private bool? GetBoolValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                if (property.ValueKind == JsonValueKind.True)
                    return true;
                if (property.ValueKind == JsonValueKind.False)
                    return false;
            }
            return null;
        }

        private DateTime? ParseDateTime(string dateString)
        {
            if (string.IsNullOrEmpty(dateString)) return null;

            if (DateTime.TryParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                return result;

            if (DateTime.TryParse(dateString, out result))
                return result;

            return null;
        }

        private decimal? ParseDecimal(string decimalString)
        {
            if (string.IsNullOrEmpty(decimalString)) return null;

            if (decimal.TryParse(decimalString, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
                return result;

            return null;
        }

        private int? ParseInt(string intString)
        {
            if (string.IsNullOrEmpty(intString)) return null;

            if (int.TryParse(intString, out var result))
                return result;

            return null;
        }
    }
}