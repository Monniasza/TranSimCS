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
    public class RoadNodeConverter(TSWorld world) : JsonConverter<RoadNode> {
        public override RoadNode ReadJson(JsonReader reader, Type objectType, RoadNode? existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var guid = existingValue?.Guid;
            var pos = existingValue?.PositionProp?.Value;
            Lane[]? lanes = null;
            var name = existingValue?.Name ?? "";
            JsonProcessor.ReadJsonObjectProperties(reader, key => {
                switch (key) {
                    case "id": guid = serializer.Deserialize<Guid>(reader); break;
                    case "pos": pos = serializer.Deserialize<ObjPos>(reader); break;
                    case "lanes":
                        Debug.Print("Token type: ", reader.TokenType);
                        lanes = serializer.DeserializeArray<Lane>(reader);
                        break;
                    case "name": name = serializer.Deserialize<string>(reader); break;
                }
            });
            if (guid == null) throw new JsonException("no guid");
            if (pos == null) throw new JsonException("no pos for node " + guid);
            if (lanes == null) throw new JsonException("no lanes for node " + guid);
            RoadNode node = new RoadNode(world, name, pos.Value, guid);
            foreach (var lane in lanes) node.AddLane(lane);
            return node;
        }

        public override void WriteJson(JsonWriter writer, RoadNode? value, JsonSerializer serializer) {
            var node = value;
            if (node is null) { writer.WriteNull(); return; }
            writer.WriteStartObject();

            // Serialize properties of the Node class
            writer.WritePropertyName("id");
            writer.WriteValue(value.Guid);
            writer.WritePropertyName("pos");
            serializer.Serialize(writer, value.PositionProp.Value);
            writer.WritePropertyName("lanes");
            serializer.Serialize(writer, value.Lanes);

            writer.WriteEndObject();
        }
    }
}
