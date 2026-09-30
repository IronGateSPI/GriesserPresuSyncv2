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
        /// El converter tolera bool, 1/0 y "si"/"no" porque no está confirmado
        /// cómo lo envía exactamente la API.
        /// </summary>
        [JsonConverter(typeof(SiNoBooleanConverter))]
        public bool pedido_online { get; set; }
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
