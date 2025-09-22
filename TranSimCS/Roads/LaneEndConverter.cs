using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranSimCS.Roads {
    public class LaneEndConverter : JsonConverter<LaneEnd> {
        public override LaneEnd ReadJson(JsonReader reader, Type objectType, LaneEnd existingValue, bool hasExistingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, LaneEnd laneEnd, JsonSerializer serializer) {
            writer.WriteStartArray();
            serializer.Serialize(writer, laneEnd.RoadNodeEnd);
            writer.WriteValue(laneEnd.lane.Index);
            writer.WriteEndArray();
        }
    }
}
