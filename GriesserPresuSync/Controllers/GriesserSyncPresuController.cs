using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GriesserPresuSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static GriesserPresuSync.Controllers.MiGriesserContext;

namespace GriesserPresuSync.Controllers
{
    public class GriesserSyncPresuController
    {
        private readonly MiGriesserApiController _apiController;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public GriesserSyncPresuController(MiGriesserApiController apiController, ILogger logger, IServiceScopeFactory serviceScopeFactory)
        {
            _apiController = apiController;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        async Task<Presupuesto[]> GetPresupuestosAsync(int initialId = -1)
        {
            return await _apiController.GetAsync<Presupuesto[]>(initialId);
        }


        public async void syncPresupuestos()
        {

            var initialId = await GetLastSyncID();

            _logger.LogInformation("Iniciando la sincronización de presupuestos");
            try
            {
                var presupuestos = await GetPresupuestosAsync(initialId);

                _logger.LogInformation($"Recuperados {presupuestos.Length} presupuestos");

                foreach (var presupuesto in presupuestos)
                {
                    try
                    {
                        guardaPresupuesto(presupuesto);
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

        private async Task<int> GetLastSyncID()
        {
            // IMPORTANTE: usamos MaxAsync con cast a int? para que EF traduzca a
            //   SELECT MAX(IdBudget) FROM IG_GriesserSyncPresupuestos
            // y NO materialice la entidad completa. Así evitamos
            // SqlNullValueException si alguna columna no anulable contiene NULL
            // (filas legacy, columnas añadidas a mano, etc.) y además es mucho
            // más eficiente que traer todas las filas a memoria.
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();
                    var maxId = await dbContext.IG_GriesserSyncPresupuestos
                        .Select(l => (int?)l.IdBudget)
                        .MaxAsync();
                    if (maxId.HasValue)
                    {
                        _logger.LogInformation($"El último ID sincronizado es: {maxId.Value}");
                        return maxId.Value;
                    }
                }
            }
            catch (Exception e)
            {
                // Si no podemos leer el último ID, devolvemos -1 para arrancar
                // desde el principio en lugar de tirar el worker.
                _logger.LogError(e, "No se pudo obtener el último ID sincronizado; se usará -1");
            }
            return -1;
        }

        private async void guardaPresupuesto(Presupuesto presupuesto)
        {

            _logger.LogInformation($"Inicio de sincronización del presupuesto {presupuesto.id_budget} - {presupuesto.presupuesto}");

            var num_linea = 0;
            foreach (var l in presupuesto.line_n)
            {
                IG_Presupuesto_LineaSincronizacion newLine = new IG_Presupuesto_LineaSincronizacion();
                newLine.IdLinea = presupuesto.presupuesto + "-" + num_linea + "-" + presupuesto.id_budget;
                newLine.Cliente = presupuesto.cod_client;
                newLine.IdBudget = presupuesto.id_budget;
                newLine.date = presupuesto.date_created;
                newLine.Articulo = l.cod_sage;
                newLine.NPresupuesto = presupuesto.presupuesto;
                newLine.NPersianas = presupuesto.num_persianas;
                newLine.TotalSup = presupuesto.total_sup;
                newLine.TotalAncho = presupuesto.total_ancho;
                newLine.TotalLargo = presupuesto.total_largo;
                newLine.LargoTapas = presupuesto.largo_tapas;
                newLine.TotalTapas = presupuesto.total_tapas;
                newLine.BK = l.bk;
                newLine.HL = l.hl;
                newLine.Accion = l.accion;
                newLine.TL = l.tl;
                newLine.Uni = l.units;
                newLine.PUnidad = l.price_per_unit;
                newLine.PUnidad2 = l.price_per_unit_with_discount;
                newLine.TEUR = l.teur;
                newLine.POS = l.pos;
                newLine.Color = presupuesto.color;
                // Nuevos Campos Linea
                newLine.title = l.title;
                newLine.price = l.price;
                newLine.price_tapa = l.price_tapa;
                newLine.con_testero = l.con_testero;
                newLine.price_testero = l.price_testero;
                newLine.tipo = l.tipo;
                // Nuevos Campos Presupuesto
                newLine.superficie = presupuesto.superficie;
                newLine.importe_color = presupuesto.importe_color;
                newLine.importe_tejido = presupuesto.importe_tejido;
                newLine.importe_lineas = presupuesto.importe_lineas;
                newLine.importe_tapas_y_testeros = presupuesto.importe_tapas_y_testeros;
                newLine.importe_automatismos = presupuesto.importe_automatismos;
                newLine.importe_incrementos = presupuesto.importe_incrementos;
                newLine.importe_transporte = presupuesto.importe_transporte;
                newLine.importe_total = presupuesto.importe_total;
                //weinor_family
                newLine.weinor_family = presupuesto.weinor_family;
                //mas campos
                newLine.GH = l.gh;
                newLine.accionamiento = presupuesto.accionamiento;
                newLine.i_line = l.i_line;
                newLine.Client_ref = presupuesto.client_ref;
                await saveLine(newLine);
                num_linea++;
            }



            _logger.LogInformation($"Guardado el presupuesto {presupuesto.presupuesto} para sincronizar");
        }

        private async Task<IG_Presupuesto_LineaSincronizacion> saveLine(IG_Presupuesto_LineaSincronizacion linea)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();

                // Comprobación de existencia con AnyAsync: EF traduce a EXISTS(...)
                // en SQL y NO materializa la entidad. Esto evita SqlNullValueException
                // si la fila ya guardada tiene NULL en alguna columna no anulable.
                var existe = await dbContext.IG_GriesserSyncPresupuestos
                    .AnyAsync(e => e.IdLinea == linea.IdLinea);

                if (!existe)
                {
                    dbContext.IG_GriesserSyncPresupuestos.Add(linea);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation($"Insertada linea {linea.IdLinea}");
                }
                else
                {
                    _logger.LogInformation($"La linea {linea.IdLinea} ya existe; se omite");
                }
            }

            return linea;
        }
    }
}