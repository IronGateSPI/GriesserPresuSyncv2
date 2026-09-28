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
        /// <summary>
        /// Identificador único de la factura: "2026/SV/12345", o "2026/12345"
        /// cuando no hay serie. El ejercicio es imprescindible porque Sage
        /// reinicia la numeración cada año — serie+número se repiten entre
        /// ejercicios y colisionarían dentro de la ventana de 24 meses.
        /// </summary>
        [JsonProperty("num_factura")]
        public string NumFactura { get; set; }

        /// <summary>
        /// Referencia de obra/proyecto (CabeceraAlbaranCliente.zNPresupuesto).
        /// Ej. "DANIEL TIGGES- PANEL".
        /// </summary>
        [JsonProperty("referencia")]
        public string Referencia { get; set; }

        /// <summary>
        /// Núm. de confirmación del pedido de origen
        /// (CabeceraAlbaranCliente.zNConfirmacionPedido). Ej. "6807117".
        /// Se envía como texto por ser un identificador, no una cantidad.
        /// </summary>
        [JsonProperty("codigo_confirmacion_pedido")]
        public string CodigoConfirmacionPedido { get; set; }

        /// <summary>Fecha de factura en formato ISO yyyy-MM-dd.</summary>
        [JsonProperty("fecha")]
        public string Fecha { get; set; }

        /// <summary>Base imponible de la factura completa.</summary>
        [JsonProperty("total")]
        public decimal Total { get; set; }

        /// <summary>Portes de la factura (ResumenCliente.ImportePortes).</summary>
        [JsonProperty("importe_transporte")]
        public decimal ImporteTransporte { get; set; }

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

        /// <summary>
        /// Base imponible de las líneas de ese artículo en la factura.
        /// Es el mismo dato que alimentaba facturacion_desglosada, de forma que
        /// ese campo puede retirarse una vez el CRM consuma "facturas".
        /// Sin filtro de familias, así que la suma de los importes de los
        /// productos debe cuadrar con "total" salvo por los portes.
        /// </summary>
        [JsonProperty("importe")]
        public decimal Importe { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }
    }
}
