using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranSimCS.Save {
    public delegate void JsonPropertyHandler(string propertyName);
    public static class JsonProcessor {
        public static void ReadJsonObjectProperties(JsonReader reader, JsonPropertyHandler action) {
            if (reader.TokenType != JsonToken.StartObject) throw new JsonSerializationException("The property must be an object");
            int count = 0;
            reader.Read();
            do {
                //reader.Read();
                if (reader.TokenType == JsonToken.PropertyName) {
                    //Hit a property
                    string? key = reader.Value?.ToString();
                    Debug.Print($"Lane key #{count}: {key}");
                    count++;
                    reader.Read();
                    //Debug.Print($"Type: {reader.TokenType}, value: {reader.Value}");
                    action.Invoke(key ?? throw new ArgumentNullException("null property name"));
                    reader.Read();
                } else if (reader.TokenType != JsonToken.EndObject)
                    throw new JsonSerializationException("Not a property name: " + reader.TokenType);
                Debug.Print($"Type: {reader.TokenType}, value: {reader.Value}");
            } while(reader.TokenType != JsonToken.EndObject);
        }

        public static void AssertType(JsonReader reader, JsonToken type) {
            if(reader.TokenType != type) throw new JsonSerializationException("wrong token type: " + reader.TokenType);
            reader.Read();
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
