using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TranSimCS.Save2 {
    public delegate void JsonPropHandler(ref Utf8JsonReader jsonReader, string name);
    public delegate void JsonArrayHandler(ref Utf8JsonReader jsonReader, int idx);

    public static partial class JsonProcessor {
        public static void ReadJsonObjectProperties(ref Utf8JsonReader reader, JsonPropHandler action, bool lenient = true) {
            AssertTokenType(ref reader, JsonTokenType.StartObject, lenient);
            while (true) {
                ForceRead(ref reader);
                switch (reader.TokenType) {
                    case JsonTokenType.PropertyName:
                        //Found a property
                        var propName = reader.GetString()!;
                        action(ref reader, propName);
                        break;
                    case JsonTokenType.EndObject:
                        return;
                    default:
                        FailTokenTypes(ref reader, JsonTokenType.PropertyName, JsonTokenType.EndObject);
                        break;
                }
            }
        }
        public static void ReadJsonArrayProperties(ref Utf8JsonReader reader, JsonArrayHandler action) {
            int i = 0;
            AssertTokenType(ref reader, JsonTokenType.StartArray);
            while (true) {
                ForceRead(ref reader);
                if (reader.TokenType == JsonTokenType.EndArray) return;
                action(ref reader, i++);
            }
        }

        public static void SkipToPropertyName(ref Utf8JsonReader reader) {
            while (true) {
                var oldReader = reader;
                ForceRead(ref reader);
                if(reader.TokenType == JsonTokenType.PropertyName) {
                    reader = oldReader;
                    return;
                }
            }
        }

        public static void ForceRead(ref Utf8JsonReader reader) {
            var success = reader.Read();
            if (!success)
                Fail(reader, "Unexpected end of JSON");
        }
        public static void AssertTokenType(ref Utf8JsonReader reader, JsonTokenType type, bool lenient = false) {
            if (lenient && reader.TokenType == type) return;
            ForceRead(ref reader);
            if (type != reader.TokenType)
                Fail(reader, $"Unxpected token: {reader.TokenType}, expected {type}");
        }
        public static void FailTokenTypes(ref Utf8JsonReader reader, params JsonTokenType[] type) {
            var stringsList = type.Select(type => type.ToString());
            var concatString = String.Join(", ", stringsList);
            Fail(reader, $"Unxpected token: {reader.TokenType}, expected {concatString}");
        }
        public static void Fail(Utf8JsonReader reader, string message, Exception? innerException = null) {
            var ln = reader.GetLineNumber();
            var cn = reader.GetColumnNumber();
            throw new JsonException($"{message} @ ({ln}, {cn})", null, ln, cn);
        }
    }
}
