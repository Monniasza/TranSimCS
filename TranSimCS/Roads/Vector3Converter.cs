using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using TranSimCS.Save2;

namespace TranSimCS.Roads {
    public class Vector3Converter : JsonConverter<Vector3> {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            Vector3 result = default(Vector3);
            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, key) => {
                switch (key) {
                    case "x":
                    case "X":
                        result.X = reader0.GetSingle();
                        break;
                    case "y":
                    case "Y":
                        result.Y = reader0.GetSingle();
                        break;
                    case "z":
                    case "Z":
                        result.Z = reader0.GetSingle();
                        break;
                }
            });
            return result;
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
