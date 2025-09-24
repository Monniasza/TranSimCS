using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranSimCS.Roads {
    public class RoadNodeEndConverter : JsonConverter<RoadNodeEnd> {

        public override RoadNodeEnd ReadJson(JsonReader reader, Type objectType, RoadNodeEnd? existingValue, bool hasExistingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
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
