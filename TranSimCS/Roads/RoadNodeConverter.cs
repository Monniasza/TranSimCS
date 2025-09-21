using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranSimCS.Roads {
    public class RoadNodeConverter : JsonConverter {

        public override bool CanConvert(Type objectType) {
            return typeof(RoadNode).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var node = (RoadNode)value;
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
