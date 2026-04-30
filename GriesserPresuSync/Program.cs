using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using GriesserPresuSync.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

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

        /// <summary>Recupera el path de ejecución (para Windows Service)</summary>
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
                .ConfigureLogging((hostContext, logging) =>
                {
                    // Cuando el proceso corre como Servicio de Windows, no hay
                    // consola disponible, así que conviene escribir al Visor
                    // de Eventos. AddEventLog sólo se activa en Windows; en
                    // dev/Linux es un no-op silencioso.
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        logging.AddEventLog(new EventLogSettings
                        {
                            SourceName = "GriesserPresuSync",
                            LogName = "Application"
                        });
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
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
