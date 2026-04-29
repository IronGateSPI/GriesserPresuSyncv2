using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MiGriesserMallorquinas.Models
{
    public class BudgetResponse
    {
        [JsonPropertyName("id_budget")]
        public int IdBudget { get; set; }

        [JsonPropertyName("date_created")]
        public string? DateCreated { get; set; }

        [JsonPropertyName("cod_client")]
        public string? CodClient { get; set; }

        [JsonPropertyName("presupuesto")]
        public string? Presupuesto { get; set; }

        [JsonPropertyName("expiration_date")]
        public string? ExpirationDate { get; set; }

        [JsonPropertyName("id_mallorquina_color")]
        public int? IdMallorquinaColor { get; set; }

        [JsonPropertyName("cod_mallorquina_color")]
        public string? CodMallorquinaColor { get; set; }

        [JsonPropertyName("id_mallorquina_acabado")]
        public int? IdMallorquinaAcabado { get; set; }

        [JsonPropertyName("cod_mallorquina_acabado")]
        public string? CodMallorquinaAcabado { get; set; }

        [JsonPropertyName("num_lines")]
        public int? NumLines { get; set; }

        [JsonPropertyName("price_lines")]
        public string? PriceLines { get; set; }

        [JsonPropertyName("num_incrementos")]
        public int? NumIncrementos { get; set; }

        [JsonPropertyName("price_incrementos")]
        public string? PriceIncrementos { get; set; }

        [JsonPropertyName("price_color")]
        public string? PriceColor { get; set; }

        [JsonPropertyName("importe_transporte")]
        public string? ImporteTransporte { get; set; }

        [JsonPropertyName("importe_instalacion")]
        public string? ImporteInstalacion { get; set; }

        [JsonPropertyName("total")]
        public string? Total { get; set; }

        [JsonPropertyName("line_n")]
        public List<BudgetLineResponse> Lines { get; set; } = new List<BudgetLineResponse>();
    }

    public class BudgetLineResponse
    {
        [JsonPropertyName("pos")]
        public string Pos { get; set; }

        [JsonPropertyName("id_mallorquina_tipo")]
        public int? IdMallorquinaTipo { get; set; }

        [JsonPropertyName("cod_mallorquina_tipo")]
        public string? CodMallorquinaTipo { get; set; }

        [JsonPropertyName("id_mallorquina_modelo")]
        public int? IdMallorquinaModelo { get; set; }

        [JsonPropertyName("cod_mallorquina_modelo")]
        public string? CodMallorquinaModelo { get; set; }

        [JsonPropertyName("ancho_hueco")]
        public int? AnchoHueco { get; set; }

        [JsonPropertyName("alto_hueco")]
        public int? AltoHueco { get; set; }

        [JsonPropertyName("id_mallorquina_esquema")]
        public int? IdMallorquinaEsquema { get; set; }

        [JsonPropertyName("cod_mallorquina_esquema")]
        public string? CodMallorquinaEsquema { get; set; }

        [JsonPropertyName("id_mallorquina_tipologia_instalacion")]
        public int? IdMallorquinaTipologiaInstalacion { get; set; }

        [JsonPropertyName("cod_mallorquina_tipologia_instalacion")]
        public string? CodMallorquinaTipologiaInstalacion { get; set; }

        [JsonPropertyName("motorizado")]
        public bool? Motorizado { get; set; }

        [JsonPropertyName("ancho_hoja")]
        public int? AnchoHoja { get; set; }

        [JsonPropertyName("alto_hoja")]
        public int? AltoHoja { get; set; }

        [JsonPropertyName("precio_por_hoja")]
        public string? PrecioPorHoja { get; set; }

        [JsonPropertyName("num_hojas")]
        public string? NumHojas { get; set; }

        [JsonPropertyName("precio_herraje")]
        public string? PrecioHerraje { get; set; }

        [JsonPropertyName("precio_incrementos")]
        public string? PrecioIncrementos { get; set; }

        [JsonPropertyName("precio_automatismos")]
        public string? PrecioAutomatismos { get; set; }

        [JsonPropertyName("units")]
        public string? Units { get; set; }

        [JsonPropertyName("total")]
        public string? Total { get; set; }

        [JsonPropertyName("id_automatismo_marca")]
        public int? IdAutomatismoMarca { get; set; }

        [JsonPropertyName("cod_automatismo_marca")]
        public string? CodAutomatismoMarca { get; set; }

        [JsonPropertyName("id_automatismo_receptor")]
        public int? IdAutomatismoReceptor { get; set; }

        [JsonPropertyName("cod_automatismo_receptor")]
        public string? CodAutomatismoReceptor { get; set; }

        [JsonPropertyName("precio_receptor")]
        public string? PrecioReceptor { get; set; }

        [JsonPropertyName("precio_mandos")]
        public string? PrecioMandos { get; set; }

        [JsonPropertyName("mando_n")]
        public List<MandoResponse> Mandos { get; set; } = new List<MandoResponse>();
    }

    public class MandoResponse
    {
        [JsonPropertyName("id_automatismo_mando")]
        public int IdAutomatismoMando { get; set; }
        [JsonPropertyName("cod_automatismo_mando")]
        public string? CodAutomatismoMando { get; set; }
        [JsonPropertyName("price")]
        public string? Price { get; set; }
        [JsonPropertyName("units")]
        public int? Units { get; set; }
        [JsonPropertyName("total")]
        public string? Total { get; set; }
    }
}