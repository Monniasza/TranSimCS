using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace TranSimCS.Save {
    public delegate void JsonPropertyHandler(string propertyName);
    public static class JsonProcessor {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public static void ReadJsonObjectProperties(JsonReader reader, JsonPropertyHandler action) {
            int count = 0;
            AssertType(reader, JsonToken.StartObject);
            while (true) {
                log.Trace($"Type of property: {reader.TokenType}, value: {reader.Value}");
                if (reader.TokenType == JsonToken.PropertyName) {
                    //Hit a property
                    string? key = reader.Value?.ToString();
                    count++;

                    log.Trace($"Type before data: {reader.TokenType}, value: {reader.Value}");
                    reader.Read();
                    action.Invoke(key ?? throw new ArgumentNullException("null property name"));
                    log.Trace($"Type after data: {reader.TokenType}, value: {reader.Value}");
                } else if (reader.TokenType == JsonToken.EndObject) {
                    return; //End of object
                } else {
                    //Loop back
                    throw new JsonSerializationException("Not a property name: " + reader.TokenType);
                }
            }
        }

        public static void Stats(JsonReader reader) => log.Trace($"Type: {reader.TokenType}, value: {reader.Value}");


        public static void AssertType(JsonReader reader, JsonToken type) {
            if (reader.TokenType != type) reader.Read();
            if (reader.TokenType != type) throw new JsonSerializationException("wrong token type: " + reader.TokenType);
        }

        public static T[]? DeserializeArray<T>(this JsonSerializer serializer, JsonReader reader) {
            var list = new List<T>();
            if (reader.TokenType != JsonToken.StartArray) throw new JsonException("Must be an array");
            while(reader.TokenType != JsonToken.EndArray) {
                reader.Read();
                if(reader.TokenType != JsonToken.EndArray) {
                    list.Add(serializer.Deserialize<T>(reader));
                }
            }
            return list.ToArray();
        }

        public static void Fail(string message, JsonReader reader) {
        }

        public static float? ReadAsFloat(this JsonReader reader) => (float?)(reader.ReadAsDecimal());
        public static double? ReadAsDouble(this JsonReader reader) => (double?)(reader.ReadAsDecimal());
    }
}
