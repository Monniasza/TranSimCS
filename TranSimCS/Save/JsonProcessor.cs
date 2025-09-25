using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranSimCS.Save {
    public delegate void JsonPropertyHandler(string propertyName);
    public static class JsonProcessor {
        public static void ReadJsonObjectProperties(JsonReader reader, JsonPropertyHandler action) {
            reader.Read();
            if (reader.TokenType != JsonToken.StartObject) throw new JsonSerializationException("The property must be an object");
            while (reader.TokenType != JsonToken.EndObject) {
                reader.Read();
                if (reader.TokenType == JsonToken.PropertyName)
                    //Hit a property
                    action.Invoke(reader.Value?.ToString() ?? throw new ArgumentNullException("null property name"));
                else if (reader.TokenType != JsonToken.EndObject)
                    throw new JsonSerializationException("Not a property name: " + reader.TokenType);
            }
        }

        public static void AssertType(JsonReader reader, JsonToken type) {
            reader.Read();
            if(reader.TokenType != type) throw new JsonSerializationException("wrong token type: " + reader.TokenType);
        }

        public static float? ReadAsFloat(this JsonReader reader) => (float?)(reader.ReadAsDecimal());
        public static double? ReadAsDouble(this JsonReader reader) => (double?)(reader.ReadAsDecimal());
    }
}
