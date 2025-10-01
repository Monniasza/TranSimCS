using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            
            // Debug output to help diagnose lookup failures
                System.Diagnostics.Debug.Print($"Looking for road node with GUID: {guid}");
            System.Diagnostics.Debug.Print($"Current nodes in world: {world.RoadNodes.Count}");
            foreach (var n in world.RoadNodes) {
            System.Diagnostics.Debug.Print($"  - {n.Guid}: {n.Name}");
            }
            
            var foundNode = world.FindRoadNodeOrNull(guid);
            if (foundNode == null) {
            throw new JsonException($"Road node with GUID {guid} not found. Make sure nodes are loaded before segments in the JSON file.");
            }
            node = foundNode;
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
