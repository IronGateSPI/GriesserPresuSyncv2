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

        /// <summary>
        /// Ventana histórica (en meses) del campo "facturas" del PUT.
        /// Default 24. Acota el tamaño del payload: con 5 años (60) el peor
        /// cliente medido generaba ~215 KB url-encoded solo en este campo.
        /// El histórico largo agregado sigue viajando en facturacion_desglosada.
        /// Cambiarlo NO requiere recompilar, solo reiniciar el servicio.
        /// </summary>
        public int FacturasHistoricoMeses { get; set; } = 24;

        /// <summary>
        /// Guarda en PayloadEnviado el JSON íntegro también en los envíos
        /// correctos, no solo cuando fallan. Default false.
        ///
        /// Pensado para depurar: se activa, se deja correr, se mira la cola y
        /// se vuelve a desactivar. Con la deduplicación del trigger cada cambio
        /// genera UNA fila en lugar de una ráfaga, así que tenerlo activo un
        /// rato cuesta ~300 KB por cambio, no los varios MB de antes.
        ///
        /// No conviene dejarlo puesto de forma permanente: la cola acumulaba
        /// 122 MB solo con el payload antiguo, que era quince veces menor.
        /// </summary>
        public bool GuardarPayloadCompleto { get; set; } = false;

        // ====== Import CRM → Sage ======
        /// <summary>Activa/desactiva el worker de bajada nocturna.</summary>
        public bool EnableImport { get; set; } = true;

        /// <summary>
        /// Hora del día (0–23) en la que se ejecuta el job nocturno. Default 3 (03:00).
        /// </summary>
        public int ImportHour { get; set; } = 3;
    }
}
