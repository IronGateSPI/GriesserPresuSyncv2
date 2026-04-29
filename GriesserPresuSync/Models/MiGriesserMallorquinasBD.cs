using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MiGriesserMallorquinasBD.Models
{
    /// <summary>
    /// Modelo para la tabla IG_GriesserSyncMallor2 (Cabecera del budget)
    /// </summary>
    public class BudgetResponseBD
    {
        public string IdLinea { get; set; }
        public int? IdBudget { get; set; }
        public string CodClient { get; set; }
        public DateTime? DateCreated { get; set; }
        public string Presupuesto { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public int? IdMallorquinaColor { get; set; }
        public string CodMallorquinaColor { get; set; }
        public int? IdMallorquinaAcabado { get; set; }
        public string CodMallorquinaAcabado { get; set; }
        public int? NumLines { get; set; }
        public decimal? PriceLines { get; set; }
        public int? NumIncrementos { get; set; }
        public decimal? PriceIncrementos { get; set; }
        public decimal? PriceColor { get; set; }
        public decimal? ImporteTransporte { get; set; }
        public decimal? ImporteInstalacion { get; set; }
        public decimal? Total { get; set; }
    }

    /// <summary>
    /// Modelo para la tabla IG_GriesserSyncMallor2_Lineas
    /// </summary>
    public class BudgetLineResponseBD
    {
        public string IdLineaPadre { get; set; }
        public int IdLinea { get; set; }
        public string Pos { get; set; }
        public int? IdMallorquinaTipo { get; set; }
        public string CodMallorquinaTipo { get; set; }
        public int? IdMallorquinaModelo { get; set; }
        public string CodMallorquinaModelo { get; set; }
        public int? AnchoHueco { get; set; }
        public int? AltoHueco { get; set; }
        public int? IdMallorquinaEsquema { get; set; }
        public string CodMallorquinaEsquema { get; set; }
        public int? IdMallorquinaTipologiaInstalacion { get; set; }
        public string CodMallorquinaTipologiaInstalacion { get; set; }
        public bool? Motorizado { get; set; }
        public int? AnchoHoja { get; set; }
        public int? AltoHoja { get; set; }
        public decimal? PrecioPorHoja { get; set; }
        public int? NumHojas { get; set; }
        public decimal? PrecioHerraje { get; set; }
        public decimal? PrecioIncrementos { get; set; }
        public decimal? PrecioAutomatismos { get; set; }
        public int? Units { get; set; }
        public decimal? Total { get; set; }
        public int? IdAutomatismoMarca { get; set; }
        public string CodAutomatismoMarca { get; set; }
        public int? IdAutomatismoReceptor { get; set; }
        public string CodAutomatismoReceptor { get; set; }
        public decimal? PrecioReceptor { get; set; }
        public decimal? PrecioMandos { get; set; }
    }

    /// <summary>
    /// Modelo para la tabla IG_GriesserSyncMallor2_Mandos
    /// </summary>
    public class MandoResponseBD
    {
        public string IdLineaPadre { get; set; }
        public int IdLinea { get; set; }
        public int IdAutomatismoMando { get; set; }
        public string CodAutomatismoMando { get; set; }
        public decimal? Price { get; set; }
        public int? Units { get; set; }
        public decimal? Total { get; set; }
    }
}