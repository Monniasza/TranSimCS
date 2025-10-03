using System;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;
using TranSimCS.Save;

namespace TranSimCS.Roads {
    public class Vector3Converter : JsonConverter<Vector3> {
        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer) {
            float x = 0f, y = 0f, z = 0f;

            JsonProcessor.ReadJsonObjectProperties(reader, key => {
                switch (key) {
                    case "x":
                    case "X":
                        x = reader.ReadAsFloat() ?? 0f;
                        break;
                    case "y":
                    case "Y":
                        y = reader.ReadAsFloat() ?? 0f;
                        break;
                    case "z":
                    case "Z":
                        z = reader.ReadAsFloat() ?? 0f;
                        break;
                }
            });

            return new Vector3(x, y, z);
        }

        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer) {
            writer.WriteStartObject();
            
            writer.WritePropertyName("x");
            writer.WriteValue(value.X);
            
            writer.WritePropertyName("y");
            writer.WriteValue(value.Y);
            
            writer.WritePropertyName("z");
            writer.WriteValue(value.Z);
            
            writer.WriteEndObject();
        }
    }
}
