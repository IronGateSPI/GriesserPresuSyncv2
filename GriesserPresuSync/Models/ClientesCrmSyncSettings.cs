using System;

namespace GriesserPresuSync.Models
{
    /// <summary>
    /// Configuración del módulo de sincronización Clientes ↔ CRM Griesser.
    /// Sigue el mismo patrón que GriesserSyncSettings / MallorquinasSyncSettings.
    ///
    /// Bloques:
    ///   - API / endpoints
    ///   - Export Sage → CRM (worker en tiempo real)
    ///   - Import CRM → Sage (worker nocturno)
    /// </summary>
    public class ClientesCrmSyncSettings
    {
        // ====== API ======
        /// <summary>
        /// Base del CRM. Las llamadas se construyen como:
        ///   PUT  {ApiUrl}/{codigoCliente}/sync_sage_distributor
        ///   GET  {ApiUrl}/all/sync_sage_distributor
        /// </summary>
        public string ApiUrl { get; set; } = "https://www.crmgriesser.es/es";

        // ====== Export Sage → CRM ======
        /// <summary>Activa/desactiva el worker que envía cambios al CRM.</summary>
        public bool EnableExport { get; set; } = true;

        /// <summary>Intervalo del worker de export (s). Default 30.</summary>
        public int SyncIntervalSeconds { get; set; } = 30;

        /// <summary>Reintentos máximos antes de pasar la fila a 'Error'.</summary>
        public int MaxIntentos { get; set; } = 5;

        /// <summary>
        /// Empresa de Sage a sincronizar. La consulta filtra por CodigoEmpresa = este valor.
        /// </summary>
        public short CodigoEmpresa { get; set; } = 1;

        /// <summary>Timeout HTTP (s) para cada llamada PUT.</summary>
        public int HttpTimeoutSeconds { get; set; } = 30;

        // ====== Import CRM → Sage ======
        /// <summary>Activa/desactiva el worker de bajada nocturna.</summary>
        public bool EnableImport { get; set; } = true;

        /// <summary>
        /// Hora del día (0–23) en la que se ejecuta el job nocturno. Default 3 (03:00).
        /// </summary>
        public int ImportHour { get; set; } = 3;
    }
}
