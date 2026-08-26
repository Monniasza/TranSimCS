using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using SixLabors.ImageSharp.PixelFormats;
using TranSimCS.Save2;

namespace TranSimCS.Roads {
    public class LaneSpecConverter : JsonConverter<LaneSpec> {
        public override LaneSpec Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            Rgba32 color = Colors.Gray;
            VehicleTypes vehicleTypes = VehicleTypes.None;
            LaneFlags flags = LaneFlags.None;
            float width = 3.5f;
            float speedLimit = 50f;
            float lineWidth = 0.2f;
            Surface surface = Surface.Tiles;

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
                    case "lineWidth":
                        lineWidth = reader0.GetSingle();
                        break;
                    case "surface":
                        surface = (Surface)(reader0.GetInt32());
                        break;
                }
            });

            return new LaneSpec(color, vehicleTypes, width, speedLimit, flags, lineWidth, surface);
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
            writer.WriteNumber("lineWidth", value.LineWidth);
            writer.WriteNumber("surface", (int)value.Surface);
            
            writer.WriteEndObject();
        }
    }
}
