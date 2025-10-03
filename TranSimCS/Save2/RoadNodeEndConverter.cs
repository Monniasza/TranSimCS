using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Roads;
using TranSimCS.Worlds;

namespace TranSimCS.Save2 {
    public class RoadNodeEndConverter : JsonConverter<RoadNodeEnd> {
        private readonly TSWorld _world;

        public RoadNodeEndConverter(TSWorld world) {
            _world = world;
        }

        public override RoadNodeEnd Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartArray) {
                throw new JsonException("Expected StartArray token");
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.String) {
                throw new JsonException("Expected String token for GUID");
            }
            Guid guid = Guid.Parse(reader.GetString()!);

            var foundNode = _world.FindRoadNodeOrNull(guid);
            if (foundNode == null) {
                throw new JsonException($"Road node with GUID {guid} not found. Make sure nodes are loaded before segments in the JSON file.");
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number) {
                throw new JsonException("Expected Number token for NodeEnd");
            }
            NodeEnd nodeEnd = (NodeEnd)reader.GetInt32();

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray) {
                throw new JsonException("Expected EndArray token");
            }

            return foundNode.GetEnd(nodeEnd);
        }

        public override void Write(Utf8JsonWriter writer, RoadNodeEnd value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();
            writer.WriteStringValue(value.Node.Guid.ToString());
            writer.WriteNumberValue((int)value.End);
            writer.WriteEndArray();
        }
    }
}
