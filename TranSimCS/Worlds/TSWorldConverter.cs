using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TranSimCS.Roads;
using TranSimCS.Save;

namespace TranSimCS.Worlds {
    public class TSWorldConverter : JsonConverter<TSWorld> {
        public override TSWorld ReadJson(JsonReader reader, Type objectType, TSWorld existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var world = existingValue ?? new TSWorld();
            Debug.Print($"Token type: {reader.TokenType}");
            JsonProcessor.ReadJsonObjectProperties(reader, key => {
                switch (key) {
                    case "nodes":
                        var nodes = serializer.Deserialize<RoadNode[]>(reader);
                        world.RoadNodes.Clear();
                        foreach(var node in nodes ?? []) world.RoadNodes.Add(node);
                        break;
                    case "segments":
                        var segments = serializer.Deserialize<RoadStrip[]>(reader);
                        world.RoadSegments.Clear();
                        foreach(var segment in segments ?? []) world.RoadSegments.Add(segment);
                        break;
                }
            });
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
