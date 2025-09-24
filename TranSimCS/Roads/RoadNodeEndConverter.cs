using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TranSimCS.Save;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public class RoadNodeEndConverter(TSWorld world) : JsonConverter<RoadNodeEnd> {

        public override RoadNodeEnd ReadJson(JsonReader reader, Type objectType, RoadNodeEnd existingValue, bool hasExistingValue, JsonSerializer serializer) {
            RoadNode node;
            NodeEnd nodeEnd;
            JsonProcessor.AssertType(reader, JsonToken.StartArray);
            var guid = serializer.Deserialize<Guid>(reader);
            node = world.FindRoadNode(guid);
            nodeEnd = serializer.Deserialize<NodeEnd>(reader);
            JsonProcessor.AssertType(reader, JsonToken.EndArray);
            return node.GetEnd(nodeEnd);
        }

        public override void WriteJson(JsonWriter writer, RoadNodeEnd? value, JsonSerializer serializer) {
            if (value is null) { writer.WriteNull(); return; }
            writer.WriteStartArray();
            serializer.Serialize(writer, value.Node.Guid);
            serializer.Serialize(writer, value.End);
            writer.WriteEndArray();
        }
    }
}
