using System;
using Newtonsoft.Json;
using TranSimCS.Save;
using Microsoft.Xna.Framework;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public class ObjPosConverter : JsonConverter<ObjPos> {
        public override ObjPos ReadJson(JsonReader reader, Type objectType, ObjPos existingValue, bool hasExistingValue, JsonSerializer serializer) {
            Vector3? position = null;
            float azimuthRadians = 0f;
            float inclination = 0f;
            float tilt = 0f;

            JsonProcessor.ReadJsonObjectProperties(reader, key => {
                switch (key) {
                    case "position":
                        position = serializer.Deserialize<Vector3>(reader);
                        break;
                    case "azimuth":
                        azimuthRadians = reader.ReadAsFloat() ?? 0f;
                        break;
                    case "inclination":
                        inclination = reader.ReadAsFloat() ?? 0f;
                        break;
                    case "tilt":
                        tilt = reader.ReadAsFloat() ?? 0f;
                        break;
                }
            });

            if (position == null) {
                throw new JsonException("Missing 'position' property in ObjPos");
            }

            // Convert azimuth from radians to field representation
            int azimuthField = Geometry.RadiansToField(azimuthRadians);
            return new ObjPos(position.Value, azimuthField, inclination, tilt);
        }

        public override void WriteJson(JsonWriter writer, ObjPos value, JsonSerializer serializer) {
            writer.WriteStartObject();

            writer.WritePropertyName("position");
            serializer.Serialize(writer, value.Position);

            // Convert azimuth from field representation to radians
            writer.WritePropertyName("azimuth");
            writer.WriteValue(Geometry.FieldToRadians(value.Azimuth));

            writer.WritePropertyName("inclination");
            writer.WriteValue(value.Inclination);

            writer.WritePropertyName("tilt");
            writer.WriteValue(value.Tilt);

            writer.WriteEndObject();
        }
    }
}
