using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranSimCS.Roads {
    public class RoadNodeConverter : JsonConverter<RoadNode> {
        public override RoadNode ReadJson(JsonReader reader, Type objectType, RoadNode? existingValue, bool hasExistingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, RoadNode? value, JsonSerializer serializer) {
            var node = value;
            if (node is null) { writer.WriteNull(); return; }
            writer.WriteStartObject();

            // Serialize properties of the Node class
            writer.WritePropertyName("id");
            writer.WriteValue(node.Guid);
            writer.WritePropertyName("pos");
            serializer.Serialize(writer, node.PositionProp.Value);
            writer.WritePropertyName("lanes");
            serializer.Serialize(writer, node.Lanes);

            writer.WriteEndObject();
        }
    }
}
