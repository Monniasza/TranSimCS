using System;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;
using TranSimCS.Save;

namespace TranSimCS.Roads {
    public class LaneSpecConverter : JsonConverter<LaneSpec> {
        public override LaneSpec ReadJson(JsonReader reader, Type objectType, LaneSpec existingValue, bool hasExistingValue, JsonSerializer serializer) {
            Color color = Color.Gray;
            VehicleTypes vehicleTypes = VehicleTypes.None;
            LaneFlags flags = LaneFlags.Forward;
            float width = 3.5f;
            float speedLimit = 50f;

            JsonProcessor.ReadJsonObjectProperties(reader, key => {
                switch (key) {
                    case "color":
                        color = serializer.Deserialize<Color>(reader);
                        break;
                    case "vehicleTypes":
                        vehicleTypes = (VehicleTypes)(reader.ReadAsInt32() ?? 0);
                        break;
                    case "flags":
                        flags = (LaneFlags)(reader.ReadAsInt32() ?? 0);
                        break;
                    case "width":
                        width = reader.ReadAsFloat() ?? 3.5f;
                        break;
                    case "speedLimit":
                        speedLimit = reader.ReadAsFloat() ?? 50f;
                        break;
                }
            });

            return new LaneSpec(color, vehicleTypes, width, speedLimit, flags);
        }

        public override void WriteJson(JsonWriter writer, LaneSpec value, JsonSerializer serializer) {
            writer.WriteStartObject();
            
            writer.WritePropertyName("color");
            serializer.Serialize(writer, value.Color);
            
            writer.WritePropertyName("vehicleTypes");
            writer.WriteValue((int)value.VehicleTypes);
            
            writer.WritePropertyName("flags");
            writer.WriteValue((int)value.Flags);
            
            writer.WritePropertyName("width");
            writer.WriteValue(value.Width);
            
            writer.WritePropertyName("speedLimit");
            writer.WriteValue(value.SpeedLimit);
            
            writer.WriteEndObject();
        }
    }
}
