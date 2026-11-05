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

        /// <summary>
        /// Pedido online: 1 / 0. Traduce ResumenCliente.IG_PedidoOnline (-1/0).
        ///
        /// La entrada agrega TODAS las facturas de ese mes para ese artículo, y
        /// unas pueden venir de pedido online y otras no. Se marca 1 si
        /// CUALQUIERA de ellas lo era: el campo NO entra en el GROUP BY, se
        /// resuelve con MIN() en SQL, que sobre -1/0 equivale a un OR.
        /// Si necesitas el dato factura a factura, está en el campo "facturas".
        /// </summary>
        [JsonProperty("pedido_online")]
        public int PedidoOnline { get; set; }
    }
}
