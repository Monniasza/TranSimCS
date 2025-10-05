using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Roads;

namespace TranSimCS.Save2 {
    public class LaneSpecConverter : JsonConverter<LaneSpec> {
        public override LaneSpec Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            Microsoft.Xna.Framework.Color? color = null;
            VehicleTypes vehicleTypes = VehicleTypes.None;
            LaneFlags flags = LaneFlags.None;
            float width = 3.5f;
            float speedLimit = 50f;

            var colorConverter = new ColorConverter();

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "color":
                        color = colorConverter.Read(ref reader0, typeof(Microsoft.Xna.Framework.Color), options);
                        break;
                    case "vehicletypes":
                        reader0.Read();
                        vehicleTypes = (VehicleTypes)reader0.GetInt32();
                        break;
                    case "flags":
                        reader0.Read();
                        flags = (LaneFlags)reader0.GetInt32();
                        break;
                    case "width":
                        reader0.Read();
                        width = reader0.GetSingle();
                        break;
                    case "speedlimit":
                        reader0.Read();
                        speedLimit = reader0.GetSingle();
                        break;
                }
            });

            if (color == null) {
                JsonProcessor.Fail(reader, "Missing color property");
            }
            return new LaneSpec(color.Value, vehicleTypes, width, speedLimit, flags);
        }

        public override void Write(Utf8JsonWriter writer, LaneSpec value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            
            writer.WritePropertyName("color");
            var colorConverter = new ColorConverter();
            colorConverter.Write(writer, value.Color, options);
            
            writer.WriteNumber("vehicleTypes", (int)value.VehicleTypes);
            writer.WriteNumber("flags", (int)value.Flags);
            writer.WriteNumber("width", value.Width);
            writer.WriteNumber("speedLimit", value.SpeedLimit);
            
            writer.WriteEndObject();
        }
    }
}
