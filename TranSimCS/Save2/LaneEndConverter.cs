using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Roads;
using TranSimCS.Worlds;

namespace TranSimCS.Save2 {
    public class LaneEndConverter : JsonConverter<LaneEnd> {
        private readonly TSWorld _world;

        public LaneEndConverter(TSWorld world) {
            _world = world;
        }

        public override LaneEnd Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            JsonProcessor.AssertTokenType(ref reader, JsonTokenType.StartArray);

            reader.Read();
            var roadNodeEndConverter = new RoadNodeEndConverter(_world);
            var roadNodeEnd = roadNodeEndConverter.Read(ref reader, typeof(RoadNodeEnd), options);

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number) {
                JsonProcessor.Fail(reader, "Expected Number token for lane index");
            }
            int laneIndex = reader.GetInt32();

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray) {
                JsonProcessor.Fail(reader, "Expected EndArray token");
            }

            if (laneIndex < 0 || laneIndex >= roadNodeEnd.Node.Lanes.Count) {
                JsonProcessor.Fail(reader, $"Invalid lane index {laneIndex} for node {roadNodeEnd.Node.Guid}");
            }

            return new LaneEnd(roadNodeEnd.End, roadNodeEnd.Node.Lanes[laneIndex]);
        }

        public override void Write(Utf8JsonWriter writer, LaneEnd value, JsonSerializerOptions options) {
            writer.WriteStartArray();
            
            var roadNodeEndConverter = new RoadNodeEndConverter(_world);
            roadNodeEndConverter.Write(writer, value.RoadNodeEnd, options);
            
            writer.WriteNumberValue(value.lane.Index);
            
            writer.WriteEndArray();
        }
    }
}
