using Newtonsoft.Json;

namespace GriesserPresuSync.Models
{
    /// <summary>
    /// Detalle de un artículo dentro de facturacion_desglosada.
    /// Se serializa como objeto JSON dentro del diccionario mes → artículo:
    ///   { "2025-01": { "METV": { "importe": 1234.56, "unidades": 5.0, "color": "RAL7016" } } }
    ///
    /// El color procede de CabeceraAlbaranCliente.zcolor.
    /// Si el mismo artículo aparece en varias cabeceras del mismo mes con distinto
    /// color, se acumulan importe y unidades y se conserva el primer color encontrado
    /// (caso poco probable en la práctica por la naturaleza del campo).
    /// </summary>
    public class ArticuloDetalleDesglose
    {
        [JsonProperty("importe")]
        public decimal Importe { get; set; }

        [JsonProperty("unidades")]
        public decimal Unidades { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }
    }
}