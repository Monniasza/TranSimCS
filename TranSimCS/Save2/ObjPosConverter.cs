using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Worlds;

namespace TranSimCS.Save2 {
    public class ObjPosConverter : JsonConverter<ObjPos> {
        public override ObjPos Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException("Expected StartObject token");
            }

            Microsoft.Xna.Framework.Vector3? position = null;
            int azimuth = 0;
            float inclination = 0f;
            float tilt = 0f;

            var vector3Converter = new Vector3Converter();

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) {
                    if (position == null) {
                        throw new JsonException("Missing position property");
                    }
                    return new ObjPos(position.Value, azimuth, inclination, tilt);
                }

                if (reader.TokenType == JsonTokenType.PropertyName) {
                    string propertyName = reader.GetString()!;
                    reader.Read();

                    switch (propertyName.ToLower()) {
                        case "position":
                            position = vector3Converter.Read(ref reader, typeof(Microsoft.Xna.Framework.Vector3), options);
                            break;
                        case "azimuth":
                            azimuth = reader.GetInt32();
                            break;
                        case "inclination":
                            inclination = reader.GetSingle();
                            break;
                        case "tilt":
                            tilt = reader.GetSingle();
                            break;
                    }
                }
            }

            throw new JsonException("Unexpected end of JSON");
        }

        public override void Write(Utf8JsonWriter writer, ObjPos value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            
            writer.WritePropertyName("position");
            var vector3Converter = new Vector3Converter();
            vector3Converter.Write(writer, value.Position, options);
            
            writer.WriteNumber("azimuth", value.Azimuth);
            writer.WriteNumber("inclination", value.Inclination);
            writer.WriteNumber("tilt", value.Tilt);
            
            writer.WriteEndObject();
        }
    }
}
