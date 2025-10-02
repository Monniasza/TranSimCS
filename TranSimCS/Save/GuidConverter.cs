using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranSimCS.Save {
    public class GuidConverter : JsonConverter<Guid> {
        public override Guid ReadJson(JsonReader reader, Type objectType, Guid existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.String) throw new JsonException("not a string");
            var value = reader.Value?.ToString() ?? "";
            reader.Read();
            return Guid.Parse(value);
        }

        public override void WriteJson(JsonWriter writer, Guid value, JsonSerializer serializer) {
            writer.WriteValue(value.ToString());
        }
    }
}
