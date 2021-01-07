using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GriesserPresuSync.Controllers;
using GriesserPresuSync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                    } catch(Exception e)
                    {

                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();

                IConfiguration c = hostContext.Configuration;
                services.AddDbContext<MiGriesserContext>(opts => {
                    var connectionString = c.GetConnectionString("DefaultConnection");
                    opts.UseSqlServer(connectionString);
                });
                })
                .UseWindowsService();
    }
}
