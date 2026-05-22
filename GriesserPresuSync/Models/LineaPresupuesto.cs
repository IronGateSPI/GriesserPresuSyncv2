using System;
namespace GriesserPresuSync.Models
{
    public class LineaPresupuesto
    {
        public string accion { get; set; }
        // altura_tapa y ancho_tapa: dimensiones de la tapa en mm/cm con decimales.
        // Nullable para tolerar líneas antiguas que no traigan el campo y evitar
        // SqlNullValueException al guardar / fallos de deserialización.
        public decimal? altura_tapa { get; set; }
        public decimal? ancho_tapa { get; set; }
        public string con_testero { get; set; }
        public int bk { get; set; }
        public string cod_sage { get; set; }
        public int? hl { get; set; }
        public float? gh { get; set; }
        public string pos { get; set; }
        public float price { get; set; }
        public float price_per_unit { get; set; }
        public float price_per_unit_with_discount { get; set; }
        public float price_tapa { get; set; }
        public float price_testero { get; set; }
        public string tipo { get; set; }
        public string title { get; set; }
        public float teur { get; set; }
        public int tl { get; set; }
        public float total { get; set; }
        public int units { get; set; }

        public int i_line { get; set; }
    }
}