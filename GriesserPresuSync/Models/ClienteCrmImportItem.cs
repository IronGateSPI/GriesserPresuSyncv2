using Newtonsoft.Json;

namespace GriesserPresuSync.Models
{
    /// <summary>
    /// DTO de cada elemento devuelto por el CRM en el endpoint
    ///   GET https://www.crmgriesser.es/es/all/sync_sage_distributor
    /// Ejemplo de respuesta:
    ///   [{"cod_sage":"000746","target_de_ventas":null,"partner_type":"Premium"}, ...]
    ///
    /// Mapeo a Sage:
    ///   cod_sage         -> dbo.Clientes.CodigoCliente   (clave de búsqueda)
    ///   target_de_ventas -> dbo.Clientes.zzTarget        (decimal, nullable)
    ///   partner_type     -> dbo.Clientes.zzTipoCliente   (varchar)
    /// </summary>
    public class ClienteCrmImportItem
    {
        [JsonProperty("cod_sage")]
        public string CodSage { get; set; }

        [JsonProperty("target_de_ventas")]
        public decimal? TargetDeVentas { get; set; }

        [JsonProperty("partner_type")]
        public string PartnerType { get; set; }
    }
}
