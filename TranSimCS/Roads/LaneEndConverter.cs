using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TranSimCS.Save;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public class LaneEndConverter(TSWorld world) : JsonConverter<LaneEnd> {
        public override LaneEnd ReadJson(JsonReader reader, Type objectType, LaneEnd existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var laneEnd = hasExistingValue ? existingValue : new LaneEnd();
            JsonProcessor.AssertType(reader, JsonToken.StartArray);
            var roadNodeEnd = serializer.Deserialize<RoadNodeEnd>(reader);
            var lane = reader.ReadAsInt32() ?? throw new JsonException("invalid lane index");
            JsonProcessor.AssertType(reader, JsonToken.EndArray);
            laneEnd.end = roadNodeEnd.End;
            laneEnd.lane = roadNodeEnd.Node.Lanes[lane];
            return laneEnd;
        }

        public override void WriteJson(JsonWriter writer, LaneEnd laneEnd, JsonSerializer serializer) {
            writer.WriteStartArray();
            serializer.Serialize(writer, laneEnd.RoadNodeEnd);
            writer.WriteValue(laneEnd.lane.Index);
            writer.WriteEndArray();
        }
    }
}
