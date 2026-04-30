using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using GriesserPresuSync.Controllers;
using GriesserPresuSync.Models;
using GriesserPresuSync.Services;  // ← AÑADIR ESTA LÍNEA
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiGriesserMallorquinas;  // ← AÑADIR para BudgetSyncProcess
using MiGriesserMallorquinas.Services;  // ← AÑADIR para BudgetService

namespace GriesserPresuSync
{
    public class Program
    {
        public static readonly IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .Build();
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        /** Recuperar el path de ejecucción */
        private static string GetBasePath()
        {
            using var processModule = Process.GetCurrentProcess().MainModule;
            return Path.GetDirectoryName(processModule?.FileName);
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    try
                    {
                        config.SetBasePath(GetBasePath());
                        config.AddJsonFile("appsettings.json");
                    }
                    catch (Exception)
                    {
                        // Ignoramos: en modo CLI ya hay base path configurado
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // === CÓDIGO ORIGINAL (NO TOCAR) ===
                    /*
                    services.AddHostedService<Worker>();
                    IConfiguration c = hostContext.Configuration;
                    services.AddDbContext<MiGriesserContext>(opts => {
                        var connectionString = c.GetConnectionString("DefaultConnection");
                        opts.UseSqlServer(connectionString);
                    });

                    services.AddSingleton<BudgetSyncProcess>(sp =>
                    {
                        var connectionString = hostContext.Configuration.GetConnectionString("DefaultConnection");
                        var apiUrl = hostContext.Configuration["MallorquinasSyncSettings:ApiUrl"];
                        return new BudgetSyncProcess(connectionString, apiUrl);
                    });

                    services.AddSingleton<HttpClient>();  // Sin paquete adicional
                    services.AddHostedService<BudgetSyncWorker>();
                    */
                    IConfiguration c = hostContext.Configuration;

                    // === DbContext único: presupuestos + mallorquinas ===
                    services.AddDbContext<MiGriesserContext>(opts =>
                    {
                        var connectionString = c.GetConnectionString("DefaultConnection");
                        opts.UseSqlServer(connectionString);
                    });

                    // === Workers ===
                    services.AddHostedService<Worker>();
                    services.AddHostedService<WorkerMallorquinas>();
                })
                .UseWindowsService();
    }
}