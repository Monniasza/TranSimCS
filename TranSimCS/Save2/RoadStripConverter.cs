using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Roads;
using TranSimCS.Worlds;

namespace TranSimCS.Save2 {
    public class RoadStripConverter : JsonConverter<RoadStrip> {
        private readonly TSWorld _world;

        public RoadStripConverter(TSWorld world) {
            _world = world;
        }

        public override RoadStrip Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException("Expected StartObject token");
            }

            RoadNodeEnd? start = null;
            RoadNodeEnd? end = null;
            List<LaneStrip> lanes = new List<LaneStrip>();
            Guid guid = Guid.Empty;

            var roadNodeEndConverter = new RoadNodeEndConverter(_world);
            var laneStripConverter = new LaneStripConverter(_world);

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) {
                    if (start == null) throw new JsonException("Missing start property");
                    if (end == null) throw new JsonException("Missing end property");

                    var roadStrip = new RoadStrip(start, end);
                    foreach (var lane in lanes) {
                        roadStrip.AddLaneStrip(lane);
                    }
                    return roadStrip;
                }

                if (reader.TokenType == JsonTokenType.PropertyName) {
                    string propertyName = reader.GetString()!;
                    reader.Read();

                    switch (propertyName.ToLower()) {
                        case "guid":
                            guid = Guid.Parse(reader.GetString()!);
                            break;
                        case "start":
                            start = roadNodeEndConverter.Read(ref reader, typeof(RoadNodeEnd), options);
                            break;
                        case "end":
                            end = roadNodeEndConverter.Read(ref reader, typeof(RoadNodeEnd), options);
                            break;
                        case "lanes":
                            if (reader.TokenType != JsonTokenType.StartArray) {
                                throw new JsonException("Expected StartArray token for lanes");
                            }
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
                                var lane = laneStripConverter.Read(ref reader, typeof(LaneStrip), options);
                                lanes.Add(lane);
                            }
                            break;
                    }
                }
            }

            throw new JsonException("Unexpected end of JSON");
        }

        public override void Write(Utf8JsonWriter writer, RoadStrip value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            
            writer.WriteString("guid", value.Guid.ToString());
            
            writer.WritePropertyName("start");
            var roadNodeEndConverter = new RoadNodeEndConverter(_world);
            roadNodeEndConverter.Write(writer, value.StartNode, options);
            
            writer.WritePropertyName("end");
            roadNodeEndConverter.Write(writer, value.EndNode, options);
            
            writer.WritePropertyName("lanes");
            writer.WriteStartArray();
            var laneStripConverter = new LaneStripConverter(_world);
            foreach (var lane in value.Lanes) {
                laneStripConverter.Write(writer, lane, options);
            }
            writer.WriteEndArray();
            
            writer.WriteEndObject();
        }
    }
}
