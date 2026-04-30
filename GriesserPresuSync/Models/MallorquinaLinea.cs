using System;

namespace GriesserPresuSync.Models
{
    /// <summary>
    /// DTO API: representa una línea dentro de un budget de mallorquina.
    /// Mismo estilo que LineaPresupuesto.cs.
    /// </summary>
    public class MallorquinaLinea
    {
        public string pos { get; set; }
        public int? id_mallorquina_tipo { get; set; }
        public string cod_mallorquina_tipo { get; set; }
        public int? id_mallorquina_modelo { get; set; }
        public string cod_mallorquina_modelo { get; set; }
        public int? ancho_hueco { get; set; }
        public int? alto_hueco { get; set; }
        public int? id_mallorquina_esquema { get; set; }
        public string cod_mallorquina_esquema { get; set; }
        public int? id_mallorquina_tipologia_instalacion { get; set; }
        public string cod_mallorquina_tipologia_instalacion { get; set; }
        public bool? motorizado { get; set; }
        public int? ancho_hoja { get; set; }
        public int? alto_hoja { get; set; }
        public decimal? precio_por_hoja { get; set; }
        public int? num_hojas { get; set; }
        public decimal? precio_herraje { get; set; }
        public decimal? precio_incrementos { get; set; }
        public decimal? precio_automatismos { get; set; }
        public int? units { get; set; }
        public decimal? total { get; set; }
        public int? id_automatismo_marca { get; set; }
        public string cod_automatismo_marca { get; set; }
        public int? id_automatismo_receptor { get; set; }
        public string cod_automatismo_receptor { get; set; }
        public decimal? precio_receptor { get; set; }
        public decimal? precio_mandos { get; set; }

        public MallorquinaMando[] mando_n { get; set; }
    }
}
