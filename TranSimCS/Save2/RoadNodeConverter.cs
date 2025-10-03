using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Roads;
using TranSimCS.Worlds;

namespace TranSimCS.Save2 {
    public class RoadNodeConverter : JsonConverter<RoadNode> {
        private readonly TSWorld _world;

        public RoadNodeConverter(TSWorld world) {
            _world = world;
        }

        public override RoadNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException("Expected StartObject token");
            }

            Guid? guid = null;
            ObjPos? pos = null;
            List<Lane> lanes = new List<Lane>();
            string name = "";

            var objPosConverter = new ObjPosConverter();
            var laneConverter = new LaneConverter();

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) {
                    if (guid == null) throw new JsonException("Missing id property");
                    if (pos == null) throw new JsonException($"Missing pos property for node {guid}");

                    RoadNode node = new RoadNode(_world, name, pos.Value, guid);
                    foreach (var lane in lanes) {
                        node.AddLane(lane);
                    }
                    return node;
                }

                if (reader.TokenType == JsonTokenType.PropertyName) {
                    string propertyName = reader.GetString()!;
                    reader.Read();

                    switch (propertyName.ToLower()) {
                        case "id":
                            guid = Guid.Parse(reader.GetString()!);
                            break;
                        case "pos":
                            pos = objPosConverter.Read(ref reader, typeof(ObjPos), options);
                            break;
                        case "lanes":
                            if (reader.TokenType != JsonTokenType.StartArray) {
                                throw new JsonException("Expected StartArray token for lanes");
                            }
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
                                var lane = laneConverter.Read(ref reader, typeof(Lane), options);
                                lanes.Add(lane);
                            }
                            break;
                        case "name":
                            name = reader.GetString() ?? "";
                            break;
                    }
                }
            }

            throw new JsonException("Unexpected end of JSON");
        }

        public override void Write(Utf8JsonWriter writer, RoadNode value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            
            writer.WriteString("id", value.Guid.ToString());
            
            writer.WritePropertyName("pos");
            var objPosConverter = new ObjPosConverter();
            objPosConverter.Write(writer, value.PositionProp.Value, options);
            
            writer.WritePropertyName("lanes");
            writer.WriteStartArray();
            var laneConverter = new LaneConverter();
            foreach (var lane in value.Lanes) {
                laneConverter.Write(writer, lane, options);
            }
            writer.WriteEndArray();
            
            if (!string.IsNullOrEmpty(value.Name)) {
                writer.WriteString("name", value.Name);
            }
            
            writer.WriteEndObject();
        }
    }
}
