using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TranSimCS.Save2 {
    public delegate void JsonPropHandler(ref Utf8JsonReader jsonReader, string name);

    public static partial class JsonProcessor {
        public static void ReadJsonObjectProperties(ref Utf8JsonReader reader, JsonPropHandler action) {
            AssertTokenType(ref reader, JsonTokenType.StartObject);
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

        public static void ForceRead(ref Utf8JsonReader reader) {
            var success = reader.Read();
            if (!success)
                Fail(reader, "Unexpected end of JSON");
        }
        public static void AssertTokenType(ref Utf8JsonReader reader, JsonTokenType type) {
            
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
