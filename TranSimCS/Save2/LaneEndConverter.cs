using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;

namespace TranSimCS.Save2 {
    public class LaneEndConverter : JsonConverter<LaneEnd> {
        private readonly TSWorld _world;

        public LaneEndConverter(TSWorld world) {
            _world = world;
        }

        public override LaneEnd Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            JsonProcessor.ForceRead(ref reader);
            switch (reader.TokenType) {
                case JsonTokenType.StartArray:
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

                    return new LaneEnd(roadNodeEnd.End, roadNodeEnd.Node.SortedLanes[laneIndex]);
                case JsonTokenType.String:
                    var str = reader.GetString();
                    NodeEnd nodeEnd = NodeEnd.Forward;
                    var polarity = str[0];
                    if (polarity == '-') nodeEnd = NodeEnd.Backward;
                    else if (polarity != '+') JsonProcessor.Fail(reader, $"Unexpected polarity {polarity}");
                    var restOfString = str.Substring(1);
                    var guid = Guid.Parse(restOfString);
                    var lane = _world.Nodes.LaneXRef[guid];
                    return new LaneEnd(nodeEnd, lane);
                default:
                    JsonProcessor.FailTokenTypes(ref reader, [JsonTokenType.StartArray, JsonTokenType.String]); //always throws
                    return default;
            }
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
