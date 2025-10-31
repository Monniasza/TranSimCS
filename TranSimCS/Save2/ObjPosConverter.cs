using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using NLog;
using TranSimCS.Worlds;

namespace TranSimCS.Save2 {
    public class ObjPosConverter : JsonConverter<ObjPos> {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public override ObjPos Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            Vector3 position = default;
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

            return new ObjPos(position, azimuth, inclination, tilt);

            throw new JsonException("Unexpected end of JSON");
        }

        public override void Write(Utf8JsonWriter writer, ObjPos value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            
            writer.WritePropertyName("position");
            var vector3Converter = new Vector3Converter();
            vector3Converter.Write(writer, value.Position, options);

            log.Info($"Azimuth: {value.Azimuth}");
            writer.WriteNumber("azimuth", value.Azimuth);
            writer.WriteNumber("inclination", value.Inclination);
            writer.WriteNumber("tilt", value.Tilt);
            
            writer.WriteEndObject();
        }
    }
}
