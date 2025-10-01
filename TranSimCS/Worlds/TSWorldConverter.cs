using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TranSimCS.Roads;
using TranSimCS.Save;

namespace TranSimCS.Worlds {
    public class TSWorldConverter : JsonConverter<TSWorld> {
        public override TSWorld ReadJson(JsonReader reader, Type objectType, TSWorld existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var world = existingValue ?? new TSWorld();
            Debug.Print($"Token type: {reader.TokenType}");

            // We need to store the segments JSON for later processing after nodes are loaded
            Newtonsoft.Json.Linq.JObject? segmentsJson = null;

            JsonProcessor.ReadJsonObjectProperties(reader, key => {
                switch (key) {
                    case "nodes":
                        var nodes = serializer.Deserialize<RoadNode[]>(reader);
                        world.RoadNodes.Clear();
                        foreach(var node in nodes ?? []) world.RoadNodes.Add(node);
                        break;
                    case "segments":
                        // Store the segments JSON for later processing
                        segmentsJson = Newtonsoft.Json.Linq.JObject.Load(reader);
                        break;
                }
            });

            // Now that nodes are loaded, deserialize segments
            if (segmentsJson != null) {
                using (var segmentReader = segmentsJson.CreateReader()) {
                    var segments = serializer.Deserialize<RoadStrip[]>(segmentReader);
                    world.RoadSegments.Clear();
                    foreach(var segment in segments ?? []) world.RoadSegments.Add(segment);
                }
            }

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
