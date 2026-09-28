using System;
using System.Globalization;
using Newtonsoft.Json;

namespace GriesserPresuSync.Models
{
    /// <summary>
    /// Serializa decimales en formato compacto, sin ceros finales.
    ///
    /// Motivo: las columnas de Sage son DECIMAL(28,10), así que un valor de
    /// 6 unidades llega a .NET como 6.0000000000 y Newtonsoft lo escribe tal
    /// cual. En un payload con miles de números eso son decenas de KB de ceros
    /// inútiles, justo en los campos que ya tenemos en el límite de tamaño.
    ///
    ///   Antes:  {"importe":2578.0815000000,"unidades":6.0000000000}
    ///   Ahora:  {"importe":2578.08,"unidades":6}
    ///
    /// Se redondea a 2 decimales, suficiente para importes en euros y unidades.
    /// Escribe el número como literal JSON (sin comillas) vía WriteRawValue.
    /// </summary>
    public class DecimalCompactoConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            var t = Nullable.GetUnderlyingType(objectType) ?? objectType;
            return t == typeof(decimal);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var d = Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
            // "0.##" elimina los decimales sobrantes: 6.00 -> "6", 2578.08 -> "2578.08"
            writer.WriteRawValue(d.ToString("0.##", CultureInfo.InvariantCulture));
        }

        /// <summary>Solo se usa para escritura; el servicio nunca deserializa estos DTO.</summary>
        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType,
                                        object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException("DecimalCompactoConverter es solo de escritura.");
        }
    }
}
