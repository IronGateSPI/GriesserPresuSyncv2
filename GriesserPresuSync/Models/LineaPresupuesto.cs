using System;
namespace GriesserPresuSync.Models
{
    public class LineaPresupuesto
    {
        public int pos { get; set; }
        public int bk { get; set; }
        public int hl { get; set; }
        public string accion { get; set; }
        public int tl { get; set; }
        public int units { get; set; }
        public float price_per_unit { get; set; }
        public float price_per_unit_with_discount { get; set; }
        public float teur { get; set; }
    }
}
