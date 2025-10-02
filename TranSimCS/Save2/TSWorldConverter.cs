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

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) {
                    return world;
                }

                if (reader.TokenType == JsonTokenType.PropertyName) {
                    string propertyName = reader.GetString()!;
                    reader.Read();

                    switch (propertyName.ToLower()) {
                        case "nodes":
                            if (reader.TokenType != JsonTokenType.StartArray) {
                                throw new JsonException("Expected StartArray token for nodes");
                            }
                            world.RoadNodes.Clear();
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
                                var node = roadNodeConverter.Read(ref reader, typeof(Roads.RoadNode), options);
                                world.RoadNodes.Add(node);
                            }
                            break;
                        case "segments":
                            if (reader.TokenType != JsonTokenType.StartArray) {
                                throw new JsonException("Expected StartArray token for segments");
                            }
                            world.RoadSegments.Clear();
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
                                var segment = roadStripConverter.Read(ref reader, typeof(Roads.RoadStrip), options);
                                world.RoadSegments.Add(segment);
                            }
                            break;
                    }
                }
            }

            throw new JsonException("Unexpected end of JSON");
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
