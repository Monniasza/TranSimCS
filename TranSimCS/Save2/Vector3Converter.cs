using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace TranSimCS.Save2 {
    public class Vector3Converter : JsonConverter<Vector3> {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            float x = 0, y = 0, z = 0;

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                reader0.Read();

                switch (propertyName.ToLower()) {
                    case "x":
                        x = reader0.GetSingle();
                        break;
                    case "y":
                        y = reader0.GetSingle();
                        break;
                    case "z":
                        z = reader0.GetSingle();
                        break;
                }
            });

            return new Vector3(x, y, z);
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
