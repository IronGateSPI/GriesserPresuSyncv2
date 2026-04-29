using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MiGriesserMallorquinas.Models;

namespace MiGriesserMallorquinas.Services
{
    public class BudgetService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public BudgetService(string baseUrl)
        {
            _httpClient = new HttpClient();
            _baseUrl = baseUrl;
        }

        /// <summary>
        /// Obtiene todos los budgets desde una fecha específica
        /// </summary>
        /// <param name="dateFrom">Fecha desde la que obtener budgets (formato: yyyy-MM-dd)</param>
        /// <returns>Diccionario con los budgets (key = id_budget)</returns>
        public async Task<Dictionary<string, BudgetResponse>> GetBudgetsFromDateAsync(DateTime dateFrom)
        {
            try
            {
                string dateStr = dateFrom.ToString("yyyy-MM-dd");
                string url = $"{_baseUrl}?date_from={dateStr}&details=true";

                Console.WriteLine($"Consultando budgets desde: {dateStr}");
                Console.WriteLine($"URL: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error en la respuesta: {errorContent}");
                    throw new HttpRequestException($"Error en la petición: {response.StatusCode} - {errorContent}");
                }

                var content = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null, // No convertir nombres (la API usa snake_case)
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };

                var budgets = JsonSerializer.Deserialize<Dictionary<string, BudgetResponse>>(content, options);

                Console.WriteLine($"Total budgets obtenidos: {budgets?.Count ?? 0}");

                return budgets ?? new Dictionary<string, BudgetResponse>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo budgets desde {dateFrom:yyyy-MM-dd}: {ex.Message}");
                return new Dictionary<string, BudgetResponse>();
            }
        }

        /// <summary>
        /// Obtiene todos los budgets sin filtro de fecha
        /// </summary>
        public async Task<Dictionary<string, BudgetResponse>> GetAllBudgetsAsync()
        {
            try
            {
                string url = $"{_baseUrl}?details=true";

                Console.WriteLine($"Consultando todos los budgets");
                Console.WriteLine($"URL: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error en la respuesta: {errorContent}");
                    throw new HttpRequestException($"Error en la petición: {response.StatusCode} - {errorContent}");
                }

                var content = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null, // No convertir nombres (la API usa snake_case)
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };

                var budgets = JsonSerializer.Deserialize<Dictionary<string, BudgetResponse>>(content, options);

                Console.WriteLine($"Total budgets obtenidos: {budgets?.Count ?? 0}");

                return budgets ?? new Dictionary<string, BudgetResponse>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo todos los budgets: {ex.Message}");
                return new Dictionary<string, BudgetResponse>();
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}