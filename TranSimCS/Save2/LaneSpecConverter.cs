using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Roads;

namespace TranSimCS.Save2 {
    public class LaneSpecConverter : JsonConverter<LaneSpec> {
        public override LaneSpec Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException("Expected StartObject token");
            }

            Microsoft.Xna.Framework.Color? color = null;
            VehicleTypes vehicleTypes = VehicleTypes.None;
            LaneFlags flags = LaneFlags.None;
            float width = 3.5f;
            float speedLimit = 50f;

            var colorConverter = new ColorConverter();

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) {
                    if (color == null) {
                        throw new JsonException("Missing color property");
                    }
                    return new LaneSpec(color.Value, vehicleTypes, width, speedLimit, flags);
                }

                if (reader.TokenType == JsonTokenType.PropertyName) {
                    string propertyName = reader.GetString()!;
                    reader.Read();

                    switch (propertyName.ToLower()) {
                        case "color":
                            color = colorConverter.Read(ref reader, typeof(Microsoft.Xna.Framework.Color), options);
                            break;
                        case "vehicletypes":
                            vehicleTypes = (VehicleTypes)reader.GetInt32();
                            break;
                        case "flags":
                            flags = (LaneFlags)reader.GetInt32();
                            break;
                        case "width":
                            width = reader.GetSingle();
                            break;
                        case "speedlimit":
                            speedLimit = reader.GetSingle();
                            break;
                    }
                }
            }

            throw new JsonException("Unexpected end of JSON");
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
