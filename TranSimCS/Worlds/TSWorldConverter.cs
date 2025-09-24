using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranSimCS.Worlds {
    public class TSWorldConverter : JsonConverter<TSWorld> {
        public override TSWorld ReadJson(JsonReader reader, Type objectType, TSWorld existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var world = existingValue ?? new TSWorld();
            world.ReadFromJSON(reader);
            return world;
        }

        public override void WriteJson(JsonWriter writer, TSWorld? value, JsonSerializer serializer) {
            if (value is null) { writer.WriteNull(); return; }
            writer.WriteStartObject();
            writer.WritePropertyName("nodes");
            serializer.Serialize(writer, value.RoadNodes);
            writer.WritePropertyName("segments");
            serializer.Serialize(writer, value.RoadSegments);
            writer.WriteEndObject();
        }
    }
}
