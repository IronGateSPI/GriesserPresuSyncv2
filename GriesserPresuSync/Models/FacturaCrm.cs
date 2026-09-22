using System.Collections.Generic;
using Newtonsoft.Json;

namespace GriesserPresuSync.Models
{
    /// <summary>
    /// Factura individual que se envía al CRM en el campo "facturas".
    /// Estructura anidada: una factura agrupa N productos, porque en Sage
    /// una factura consolida líneas de uno o varios albaranes.
    ///
    /// Origen: ResumenCliente (cabecera) + LineasAlbaranCliente (detalle)
    ///         + CabeceraAlbaranCliente.zColor (color).
    ///
    /// Ventana histórica configurable vía ClientesCrmSyncSettings.FacturasHistoricoMeses
    /// (default 24 meses) para acotar el tamaño del payload.
    /// </summary>
    public class FacturaCrm
    {
        /// <summary>Serie + número (ej. "SV/12345") o solo número si no hay serie.</summary>
        [JsonProperty("num_factura")]
        public string NumFactura { get; set; }

        /// <summary>Referencia del pedido del cliente (LineasAlbaranCliente.SuPedido).</summary>
        [JsonProperty("referencia_cliente")]
        public string ReferenciaCliente { get; set; }

        /// <summary>Fecha de factura en formato ISO yyyy-MM-dd.</summary>
        [JsonProperty("fecha")]
        public string Fecha { get; set; }

        /// <summary>Base imponible de la factura completa.</summary>
        [JsonProperty("total")]
        public decimal Total { get; set; }

        [JsonProperty("productos")]
        public List<FacturaProductoCrm> Productos { get; set; } = new List<FacturaProductoCrm>();
    }

    /// <summary>Línea de producto dentro de una factura.</summary>
    public class FacturaProductoCrm
    {
        [JsonProperty("producto")]
        public string Producto { get; set; }

        [JsonProperty("unidades")]
        public decimal Unidades { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }
    }
}
