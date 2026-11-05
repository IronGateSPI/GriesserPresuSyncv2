using System;
using Newtonsoft.Json;

namespace GriesserPresuSync.Models
{
    public class Presupuesto
    {
        public int id_budget { get; set; }
        public string? cod_client { get; set; }
        public string? client_ref { get; set; }
        public bool weinor_family { get; set; }

        /// <summary>
        /// Indica si el presupuesto procede de un pedido online.
        /// Se persiste en Sage como IG_PedidoOnline (-1 sí / 0 no) y viaja al
        /// CRM en el campo "pedido_online" de los pedidos pendientes.
        /// La API envía normalmente 1 o null. Es ANULABLE a propósito: así el
        /// null se deserializa de forma nativa y no dependemos de que Newtonsoft
        /// invoque el converter sobre un token nulo, que con un bool no anulable
        /// lanzaría "Error converting value {null} to type 'System.Boolean'" y
        /// tumbaría el presupuesto completo.
        /// El converter cubre además 1/0, true/false y "si"/"no".
        /// Ausente o null se interpreta como "no es pedido online".
        /// </summary>
        [JsonConverter(typeof(SiNoBooleanConverter))]
        public bool? pedido_online { get; set; }
        public DateTime date_created { get; set; }
        public string presupuesto { get; set; }
        public int num_persianas { get; set; }
        public string accionamiento { get; set; }
        public float total_sup { get; set; }
        public float total_ancho { get; set; }
        public float total_largo { get; set; }
        public float largo_tapas { get; set; }
        public int total_tapas { get; set; }
        public string color { get; set; }
        public DateTime expiration_date { get; set; }
        public int num_lineas { get; set; }
        public float? superficie { get; set; }
        public float? importe_color { get; set; }
        public float? importe_tejido { get; set; }
        public float importe_lineas { get; set; }
        public float? importe_tapas_y_testeros { get; set; }
        public float? importe_automatismos { get; set; }
        public float? importe_incrementos { get; set; }
        public float? importe_transporte { get; set; }
        public float importe_total { get; set; }
        public LineaPresupuesto[] line_n { get; set; }
    }
}
