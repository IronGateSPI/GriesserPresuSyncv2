using System;

namespace GriesserPresuSync.Models
{
    /// <summary>
    /// Configuración de sincronización para Mallorquinas.
    /// Mantiene el mismo patrón que GriesserSyncSettings (presupuestos).
    /// </summary>
    public class MallorquinasSyncSettings
    {
        // API Settings
        public string ApiUrl { get; set; } = "https://www.migriesser.com/es/mallorquinas_budgets/api";
        public int details { get; set; } = 1;
        public int regs_per_page { get; set; } = 100;
        public int page { get; set; } = 1;
        public int id_from { get; set; } = 1;

        // DB Settings
        public string DestinationTable { get; set; } = "IG_GriesserSyncMallor";

        public MallorquinasSyncSettings()
        {
        }
    }
}
