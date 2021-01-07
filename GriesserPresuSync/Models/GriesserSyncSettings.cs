using System;
namespace GriesserPresuSync.Models
{
    public class GriesserSyncSettings
    {
        // API Settings
        public string ApiUrl = "https://www.migriesser.com/es/budgets/api/";
        public int details = 1;
        public int regs_per_page = 100;
        public int page = 1;
        public int id_from { get; set; } = 1;
        // DB Settings
        public string SageConnectionString { get; set; }
        public string DestinationTable = "IG_GriesserSyncPresupuestos";

        public GriesserSyncSettings()
        {
        }
        
    }
}
