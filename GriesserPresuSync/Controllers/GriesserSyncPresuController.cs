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
    
                foreach(var presupuesto in presupuestos)
                {
                    try
                    {
                        guardaPresupuesto(presupuesto);
                    } catch(Exception e)
                    {
                        _logger.LogError(e.Message);
                    }
                }

            } catch(Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        private async Task<int> GetLastSyncID()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MiGriesserContext>();
                var lastInsertedId = await dbContext.IG_GriesserSyncPresupuestos.OrderByDescending(l => l.IdBudget).ToListAsync<IG_Presupuesto_LineaSincronizacion>();
                if(lastInsertedId != null && lastInsertedId.Count > 0)
                {
                    var lastId = lastInsertedId[0].IdBudget;
                    _logger.LogInformation($"El último ID sincronizado es: {lastId}");
                    return lastId;
                }
            }
            return -1;
        }

        private async void guardaPresupuesto(Presupuesto presupuesto)
        {

            _logger.LogInformation($"Inicio de sincronización del presupuesto {presupuesto.id_budget} - {presupuesto.presupuesto}");

            var num_linea = 0;
            foreach(var l in presupuesto.line_n)
            {
                IG_Presupuesto_LineaSincronizacion newLine = new IG_Presupuesto_LineaSincronizacion();
                newLine.IdLinea = presupuesto.presupuesto + "-" + num_linea +"-" + presupuesto.id_budget;
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

                var lineaExistente = await dbContext.IG_GriesserSyncPresupuestos.SingleOrDefaultAsync(e => e.IdLinea == linea.IdLinea);

                if (lineaExistente == null)
                {
                    dbContext.IG_GriesserSyncPresupuestos.Add(linea);
                    dbContext.SaveChanges();
                    _logger.LogInformation($"Insertada linea {linea.IdLinea}");
                } else
                {
                    _logger.LogError($"La linea {linea.IdLinea} ya existe");
                }

                
            }

            return linea;
        }
    }
}
