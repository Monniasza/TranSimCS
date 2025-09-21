using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranSimCS.Roads {
    public class LaneStripConverter : JsonConverter<LaneStrip> {

        public override LaneStrip ReadJson(JsonReader reader, Type objectType, LaneStrip existingValue, bool hasExistingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, LaneStrip value, JsonSerializer serializer) {
            writer.WriteStartObject();
            writer.WritePropertyName("start");
            serializer.Serialize(writer, value.StartLane);
            writer.WritePropertyName("end");
            serializer.Serialize(writer, value.EndLane);
            writer.WritePropertyName("spec");
            serializer.Serialize(writer, value.Spec);
        }
    }
}
