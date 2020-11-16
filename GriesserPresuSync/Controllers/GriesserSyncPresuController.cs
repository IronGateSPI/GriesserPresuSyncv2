using System;
using System.Net.Http;
using System.Threading.Tasks;
using GriesserPresuSync.Models;
using Microsoft.Extensions.Logging;

namespace GriesserPresuSync.Controllers
{
    public class GriesserSyncPresuController
    {
        private readonly MiGriesserApiController _apiController;
        private readonly ILogger _logger;

        public GriesserSyncPresuController(MiGriesserApiController apiController, ILogger logger)
        {
            _apiController = apiController;
            _logger = logger;
        }

        async Task<Presupuesto[]> GetPresupuestosAsync()
        {
            return await _apiController.GetAsync<Presupuesto[]>();
        }

        public async void syncPresupuestos()
        {
            _logger.LogInformation("Iniciando la sincronización de presupuestos");
            try
            {
                var presupuestos = await GetPresupuestosAsync();

                _logger.LogInformation($"Recuperados {presupuestos.Length} presupuestos");
    
                foreach(var presupuesto in presupuestos)
                {

                }

            } catch(Exception e)
            {
                _logger.LogError(e.Message);
            }
        }
    }
}
