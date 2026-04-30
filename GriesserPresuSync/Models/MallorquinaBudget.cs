using System;

namespace GriesserPresuSync.Models
{
    /// <summary>
    /// DTO API: representa la cabecera de un budget de mallorquina con sus líneas.
    /// Mismo estilo que Presupuesto.cs.
    /// </summary>
    public class MallorquinaBudget
    {
        public int id_budget { get; set; }
        public string cod_client { get; set; }
        public DateTime? date_created { get; set; }
        public string presupuesto { get; set; }
        public DateTime? expiration_date { get; set; }
        public int? id_mallorquina_color { get; set; }
        public string cod_mallorquina_color { get; set; }
        public int? id_mallorquina_acabado { get; set; }
        public string cod_mallorquina_acabado { get; set; }
        public int? num_lines { get; set; }
        public decimal? price_lines { get; set; }
        public int? num_incrementos { get; set; }
        public decimal? price_incrementos { get; set; }
        public decimal? price_color { get; set; }
        public decimal? importe_transporte { get; set; }
        public decimal? importe_instalacion { get; set; }
        public decimal? total { get; set; }

        public MallorquinaLinea[] line_n { get; set; }
    }
}
