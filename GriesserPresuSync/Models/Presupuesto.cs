using System;
namespace GriesserPresuSync.Models
{
    public class Presupuesto
    {
        public int id_budget { get; set; }
        public DateTime date_created { get; set; }
        public string code_product { get; set; }
        public string presupuesto { get; set; }
        public int num_persianas { get; set; }
        public float total_sup { get; set; }
        public float total_ancho { get; set; }
        public float total_largo { get; set; }
        public float largo_tapas { get; set; }
        public int total_tapas { get; set; }
        public string color { get; set; }
        public LineaPresupuesto[] line_n { get; set; }
    }
}
