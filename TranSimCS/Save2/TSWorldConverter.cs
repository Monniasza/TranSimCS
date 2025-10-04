using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Worlds;

namespace TranSimCS.Save2 {
    public class TSWorldConverter : JsonConverter<TSWorld> {
        public override TSWorld Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var world = new TSWorld();
            
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException("Expected StartObject token");
            }

            var roadNodeConverter = new RoadNodeConverter(world);
            var roadStripConverter = new RoadStripConverter(world);


            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "nodes":
                        world.RoadNodes.Clear();
                        JsonProcessor.ReadJsonArrayProperties(ref reader0, (ref reader1, idx) => {
                            var node = roadNodeConverter.Read(ref reader1, typeof(Roads.RoadNode), options);
                            world.RoadNodes.Add(node);
                        });
                        break;
                    case "segments":
                        world.RoadSegments.Clear();
                        while (reader0.Read() && reader0.TokenType != JsonTokenType.EndArray) {
                            var segment = roadStripConverter.Read(ref reader0, typeof(Roads.RoadStrip), options);
                            world.RoadSegments.Add(segment);
                        }
                        break;
                }
            }, true);

            return world;
        }

        public override void Write(Utf8JsonWriter writer, TSWorld value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            
            writer.WritePropertyName("nodes");
            writer.WriteStartArray();
            var roadNodeConverter = new RoadNodeConverter(value);
            foreach (var node in value.RoadNodes) {
                roadNodeConverter.Write(writer, node, options);
            }
            writer.WriteEndArray();
            
            writer.WritePropertyName("segments");
            writer.WriteStartArray();
            var roadStripConverter = new RoadStripConverter(value);
            foreach (var segment in value.RoadSegments) {
                roadStripConverter.Write(writer, segment, options);
            }
            writer.WriteEndArray();
            
            writer.WriteEndObject();
        }
    }
}
