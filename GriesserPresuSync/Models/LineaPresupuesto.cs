using System;
namespace GriesserPresuSync.Models
{
    public class LineaPresupuesto
    {
        public string accion { get; set; }
        public string altura_tapa { get; set; }
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
    }
}
