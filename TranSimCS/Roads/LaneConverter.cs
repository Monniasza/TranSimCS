using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TranSimCS.Save;

namespace TranSimCS.Roads {
    public class LaneConverter : JsonConverter<Lane> {
        public override Lane ReadJson(JsonReader reader, Type objectType, Lane existingValue, bool hasExistingValue, JsonSerializer serializer) {
            float leftStart = existingValue?.LeftPosition ?? 0;
            float rightStart = existingValue?.RightPosition ?? 0;
            LaneSpec spec = existingValue?.Spec ?? LaneSpec.None;
            JsonProcessor.ReadJsonObjectProperties(reader, (name) => {
                switch (name) {
                    case "left": leftStart = reader.ReadAsFloat() ?? leftStart; break;
                    case "right": rightStart = reader.ReadAsFloat() ?? rightStart; break;
                    case "spec": spec = serializer.Deserialize<LaneSpec>(reader); break;
                }
            });
            return new Lane() {
                LeftPosition = leftStart,
                RightPosition = rightStart,
                Spec = spec
            };
        }

        public override void WriteJson(JsonWriter writer, Lane lane, JsonSerializer serializer) {
            writer.WriteStartObject();

            // Serialize properties of the Lane class
            writer.WritePropertyName("left");
            writer.WriteValue(lane.LeftPosition);
            writer.WritePropertyName("right");
            writer.WriteValue(lane.RightPosition);
            writer.WritePropertyName("spec");
            serializer.Serialize(writer, lane.Spec);

            writer.WriteEndObject();
        }
    }
}
