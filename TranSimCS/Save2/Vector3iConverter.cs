using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Save2 {
    public class Vector3iConverter : JsonConverter<Vector3i> {
        public override Vector3i Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            int x = 0, y = 0, z = 0;

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                reader0.Read();

                switch (propertyName.ToLower()) {
                    case "x":
                        x = reader0.GetInt32();
                        break;
                    case "y":
                        y = reader0.GetInt32();
                        break;
                    case "z":
                        z = reader0.GetInt32();
                        break;
                }
            });

            return new Vector3i(x, y, z);
        }

        public override void Write(Utf8JsonWriter writer, Vector3i value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteNumber("z", value.z);
            writer.WriteEndObject();
        }
    }
}
