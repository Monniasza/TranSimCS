using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranSimCS.Roads {
    internal class RoadStripConverter : JsonConverter<RoadStrip> {
        public override RoadStrip ReadJson(JsonReader reader, Type objectType, RoadStrip? existingValue, bool hasExistingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, RoadStrip? value, JsonSerializer serializer) {
            if (value is null) { writer.WriteNull(); return; }
            writer.WriteStartObject();
            writer.WritePropertyName("lanes");
            serializer.Serialize(writer, value.Lanes);
            writer.WriteEndObject();
        }
    }
}
