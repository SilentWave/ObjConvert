using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json;

namespace Arctron.Gltf
{
    /// <summary>
    /// Convert to int[] when all values are equals integers
    /// </summary>
    public class DoubleArrayJsonConverter : JsonConverter<Double[]>
    {
        public override Double[] ReadJson(JsonReader reader, Type objectType,
            Double[] existingValue, Boolean hasExistingValue, JsonSerializer serializer)
        {
            var values = new List<Double>();
            Double? val;
            while((val = reader.ReadAsDouble()) != null)
            {
                values.Add(val.Value);
            }
            if (values.Count > 0)
            {
                return values.ToArray();
            }
            return existingValue;
        }

        public override void WriteJson(JsonWriter writer,
            Double[] value, JsonSerializer serializer)
        {
            if (value != null)
            {
                //TODO: Very bad need a string builder at least, but this whole converter isn't that useful, json number handling is quite good already
                var json = "[" + String.Join(",", 
                    value.Select(c => Math.Round(c) - c == 0 ? (Int32)c : c)) + "]";
                writer.WriteRawValue(json);
            }
            
        }
    }
}
