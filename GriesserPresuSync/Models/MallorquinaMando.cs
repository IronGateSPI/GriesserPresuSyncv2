using System;

namespace GriesserPresuSync.Models
{
    /// <summary>
    /// DTO API: representa un mando dentro de una línea de mallorquina.
    /// Sigue el mismo estilo que LineaPresupuesto (snake_case directo).
    /// </summary>
    public class MallorquinaMando
    {
        public int id_automatismo_mando { get; set; }
        public string cod_automatismo_mando { get; set; }
        public decimal? price { get; set; }
        public int? units { get; set; }
        public decimal? total { get; set; }
    }
}
