using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace TranSimCS.Save2 {
    public class Vector3Converter : JsonConverter<Vector3> {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException("Expected StartObject token");
            }

            float x = 0, y = 0, z = 0;

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) {
                    return new Vector3(x, y, z);
                }

                if (reader.TokenType == JsonTokenType.PropertyName) {
                    string propertyName = reader.GetString()!;
                    reader.Read();

                    switch (propertyName.ToLower()) {
                        case "x":
                            x = reader.GetSingle();
                            break;
                        case "y":
                            y = reader.GetSingle();
                            break;
                        case "z":
                            z = reader.GetSingle();
                            break;
                    }
                }
            }

            throw new JsonException("Unexpected end of JSON");
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteNumber("z", value.Z);
            writer.WriteEndObject();
        }
    }
}
