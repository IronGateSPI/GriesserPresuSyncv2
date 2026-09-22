using System.Collections.Generic;
using Newtonsoft.Json;

namespace GriesserPresuSync.Models
{
    /// <summary>
    /// Pedido de venta pendiente de servir que se envía al CRM en el campo "pedidos".
    ///
    /// Origen: CabeceraPedidoCliente + LineasPedidoCliente.
    /// Filtro: CabeceraPedidoCliente.Estado = 0 (pendiente). Estado = 2 es servido.
    ///
    /// Nota sobre Unidades vs NumPersianas: son datos DISTINTOS y ambos se envían.
    /// Verificado en BD: el pedido 2025/1392 tiene zTotalPersianas = 7 mientras sus
    /// 15 líneas suman 18 unidades. "Unidades" es la suma de las líneas;
    /// "NumPersianas" es el contador propio de Griesser (zTotalPersianas).
    /// </summary>
    public class PedidoCrm
    {
        /// <summary>Fecha del pedido en formato ISO yyyy-MM-dd.</summary>
        [JsonProperty("fecha")]
        public string Fecha { get; set; }

        /// <summary>Núm. de confirmación del proveedor (zNConfirmacionPedido).</summary>
        [JsonProperty("num_confirmacion")]
        public string NumConfirmacion { get; set; }

        /// <summary>Ref. pedido cliente (CabeceraPedidoCliente.SuPedido).</summary>
        [JsonProperty("referencia_cliente")]
        public string ReferenciaCliente { get; set; }

        /// <summary>Suma de unidades de todas las líneas del pedido.</summary>
        [JsonProperty("unidades")]
        public decimal Unidades { get; set; }

        /// <summary>Contador de persianas de Griesser (zTotalPersianas).</summary>
        [JsonProperty("num_persianas")]
        public int NumPersianas { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("base_imponible")]
        public decimal BaseImponible { get; set; }

        /// <summary>Portes (CabeceraPedidoCliente.ImportePortes).</summary>
        [JsonProperty("importe_transporte")]
        public decimal ImporteTransporte { get; set; }

        /// <summary>Importe de instalación (zImporteInstalacion).</summary>
        [JsonProperty("instalacion")]
        public decimal Instalacion { get; set; }

        [JsonProperty("lineas")]
        public List<PedidoLineaCrm> Lineas { get; set; } = new List<PedidoLineaCrm>();
    }

    /// <summary>Línea de un pedido de venta.</summary>
    public class PedidoLineaCrm
    {
        [JsonProperty("producto")]
        public string Producto { get; set; }

        [JsonProperty("unidades")]
        public decimal Unidades { get; set; }

        /// <summary>Precio unidad (LineasPedidoCliente.zPrecioUnidad).</summary>
        [JsonProperty("precio")]
        public decimal Precio { get; set; }
    }
}
