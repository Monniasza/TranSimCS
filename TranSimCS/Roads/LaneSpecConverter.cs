using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using TranSimCS.Save2;

namespace TranSimCS.Roads {
    public class LaneSpecConverter : JsonConverter<LaneSpec> {
        public override LaneSpec Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            Color color = Color.Gray;
            VehicleTypes vehicleTypes = VehicleTypes.None;
            LaneFlags flags = LaneFlags.Forward;
            float width = 3.5f;
            float speedLimit = 50f;

            var colorConverter = new ColorConverter();

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, key) => {
                switch (key) {
                    case "color":
                        color = colorConverter.Read(ref reader0, typeof(Color), options);
                        break;
                    case "vehicleTypes":
                        vehicleTypes = (VehicleTypes)(reader0.GetInt32());
                        break;
                    case "flags":
                        flags = (LaneFlags)(reader0.GetInt32());
                        break;
                    case "width":
                        width = reader0.GetSingle();
                        break;
                    case "speedLimit":
                        speedLimit = reader0.GetSingle();
                        break;
                }
            });

            return new LaneSpec(color, vehicleTypes, width, speedLimit, flags);
        }

        public override void Write(Utf8JsonWriter writer, LaneSpec value, JsonSerializerOptions options) {
            writer.WriteStartObject();

            var colorConverter = new ColorConverter();
            writer.WritePropertyName("color");
            colorConverter.Write(writer, value.Color, options);
            writer.WriteNumber("vehicleTypes", (int)value.VehicleTypes);
            writer.WriteNumber("flags", (int)value.Flags);
            writer.WriteNumber("width", value.Width);
            writer.WriteNumber("speedLimit", value.SpeedLimit);

            writer.WriteEndObject();
        }
    }
}
