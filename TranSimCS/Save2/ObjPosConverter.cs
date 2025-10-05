using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Worlds;

namespace TranSimCS.Save2 {
    public class ObjPosConverter : JsonConverter<ObjPos> {
        public override ObjPos Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            Microsoft.Xna.Framework.Vector3? position = null;
            int azimuth = 0;
            float inclination = 0f;
            float tilt = 0f;

            var vector3Converter = new Vector3Converter();

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "position":
                        position = vector3Converter.Read(ref reader0, typeof(Microsoft.Xna.Framework.Vector3), options);
                        break;
                    case "azimuth":
                        reader0.Read();
                        azimuth = reader0.GetInt32();
                        break;
                    case "inclination":
                        reader0.Read();
                        inclination = reader0.GetSingle();
                        break;
                    case "tilt":
                        reader0.Read();
                        tilt = reader0.GetSingle();
                        break;
                }
            });

            return new ObjPos(position.Value, azimuth, inclination, tilt);

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
