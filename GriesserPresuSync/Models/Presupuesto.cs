using System;
namespace GriesserPresuSync.Models
{
    public class Presupuesto
    {
        public int id_budget { get; set; }
        public string? cod_client { get; set; }
        public DateTime date_created { get; set; }
        public string presupuesto { get; set; }
        public int num_persianas { get; set; }
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
