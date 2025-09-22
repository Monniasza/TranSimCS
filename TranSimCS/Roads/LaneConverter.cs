using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranSimCS.Roads {
    public class LaneConverter : JsonConverter<Lane> {
        public override Lane ReadJson(JsonReader reader, Type objectType, Lane existingValue, bool hasExistingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
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
