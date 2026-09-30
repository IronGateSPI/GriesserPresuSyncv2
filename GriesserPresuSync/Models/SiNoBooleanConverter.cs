using System;
using Newtonsoft.Json;

namespace GriesserPresuSync.Models
{
    /// <summary>
    /// Convierte a bool los campos "sí/no" de la API de MiGriesser, aceptando
    /// todas las representaciones razonables en lugar de asumir una.
    ///
    /// Motivo: el campo pedido_online se describió como "de tipo si/no" sin
    /// concretar cómo viaja en el JSON, y la API requiere autenticación para
    /// inspeccionarla. Un bool a secas rompería si llegara como "si"/"no" o
    /// como 1/0, y el fallo sería silencioso (excepción de deserialización que
    /// tumba el presupuesto entero).
    ///
    /// Acepta: true/false, 1/0, "1"/"0", "true"/"false", "si"/"sí"/"no",
    /// "y"/"n", "s"/"n". Cualquier otra cosa, incluido null, devuelve false.
    /// </summary>
    public class SiNoBooleanConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            var t = Nullable.GetUnderlyingType(objectType) ?? objectType;
            return t == typeof(bool);
        }

        public override object ReadJson(JsonReader reader, Type objectType,
                                        object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Boolean:
                    return Convert.ToBoolean(reader.Value);

                case JsonToken.Integer:
                case JsonToken.Float:
                    // Sage usa -1 para verdadero; la API podría usar 1.
                    // Cualquier valor distinto de 0 se considera verdadero.
                    return Convert.ToDecimal(reader.Value) != 0m;

                case JsonToken.String:
                    var s = (reader.Value?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
                    return s == "1" || s == "-1" || s == "true"
                        || s == "si" || s == "sí" || s == "s"
                        || s == "y"  || s == "yes";

                default:
                    return false;
            }
        }

        /// <summary>Solo se usa al leer la API; nunca serializamos hacia ella.</summary>
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException("SiNoBooleanConverter es solo de lectura.");
        }
    }
}
