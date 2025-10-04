using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using TranSimCS.Save2;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public class ObjPosConverter : JsonConverter<ObjPos> {
        public override ObjPos Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            Vector3? position = null;
            float azimuthRadians = 0f;
            float inclination = 0f;
            float tilt = 0f;

            var vectorConverter = new Vector3Converter();

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, key) => {
                switch (key) {
                    case "position":
                        position = vectorConverter.Read(ref reader0, typeof(Vector3), options);
                        break;
                    case "azimuth":
                        azimuthRadians = reader0.GetSingle();
                        break;
                    case "inclination":
                        inclination = reader0.GetSingle();
                        break;
                    case "tilt":
                        tilt = reader0.GetSingle();
                        break;
                }
            });
            if (position == null) JsonProcessor.Fail(reader, "Missing 'position' property in ObjPos");

            // Convert azimuth from radians to field representation
            int azimuthField = Geometry.RadiansToField(azimuthRadians);
            return new ObjPos(position.Value, azimuthField, inclination, tilt);
        }

        public override void Write(Utf8JsonWriter writer, ObjPos value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            var vectorSerializer = new Vector3Converter();
            writer.WritePropertyName("position");
            vectorSerializer.Write(writer, value.Position, options);
            writer.WriteNumber("azimuth", Geometry.FieldToRadians(value.Azimuth));
            writer.WriteNumber("inclination", value.Inclination);
            writer.WriteNumber("tilt", value.Tilt);
            writer.WriteEndObject();
        }
    }
}
