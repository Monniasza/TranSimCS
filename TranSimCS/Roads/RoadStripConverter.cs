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
    internal class RoadStripConverter() : JsonConverter<RoadStrip> {
        public override RoadStrip ReadJson(JsonReader reader, Type objectType, RoadStrip existingValue, bool hasExistingValue, JsonSerializer serializer) {
            RoadNodeEnd start = null;
            RoadNodeEnd end = null;
            LaneStrip[] lanes = null;
            Guid guid = Guid.Empty;

            if (existingValue != null) {
                start = existingValue.StartNode;
                end = existingValue.EndNode;
                lanes = existingValue.Lanes.ToArray();
                guid = existingValue.Guid;
            }

            JsonProcessor.ReadJsonObjectProperties(reader, key => {
                switch (key) {
                    case "start": start = serializer.Deserialize<RoadNodeEnd>(reader); break;
                    case "end": end = serializer.Deserialize<RoadNodeEnd>(reader); break;
                    case "lanes": lanes = serializer.Deserialize<LaneStrip[]>(reader); break;
                    case "guid": guid = serializer.Deserialize<Guid>(reader); Debug.Print($"Reading road strip {guid}"); break;
                }
            });

            if (start == null) throw new JsonException("no start");
            if (end == null) throw new JsonException("no end");
            if (lanes == null) throw new JsonException("no lane list");

            var roadStrip = new RoadStrip(start, end);
            foreach (var lane in lanes) roadStrip.AddLaneStrip(lane);
            return roadStrip;
        }

        public override void WriteJson(JsonWriter writer, RoadStrip? value, JsonSerializer serializer) {
            if (value is null) { writer.WriteNull(); return; }
            writer.WriteStartObject();
            writer.WritePropertyName("guid");
            serializer.Serialize(writer, value.Guid);
            writer.WritePropertyName("start");
            serializer.Serialize(writer, value.StartNode);
            writer.WritePropertyName("end");
            serializer.Serialize(writer, value.EndNode);
            writer.WritePropertyName("lanes");
            serializer.Serialize(writer, value.Lanes);
            writer.WriteEndObject();
        }
    }
}
