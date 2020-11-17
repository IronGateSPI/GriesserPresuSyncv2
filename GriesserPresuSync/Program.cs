using System;
using System.Collections.Generic;
using System.Linq;
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
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;
                    GriesserSyncSettings syncSettings = configuration.GetSection("GriesserSyncSettings").Get<GriesserSyncSettings>();
                    services.AddDbContext<MiGriesserContext>(opts =>
                        opts.UseSqlServer(syncSettings.SageConnectionString));
                    services.AddSingleton(syncSettings);
                    
                    services.AddHostedService<Worker>();

                    MiGriesserDataStore.Instance.SyncSettings = syncSettings;

                });
    }
}
