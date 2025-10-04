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
            RoadNodeEnd? start = null;
            RoadNodeEnd? end = null;
            List<LaneStrip> lanes = new List<LaneStrip>();
            Guid guid = Guid.Empty;

            var roadNodeEndConverter = new RoadNodeEndConverter(_world);
            var laneStripConverter = new LaneStripConverter(_world);

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "guid":
                        reader0.Read();
                        guid = Guid.Parse(reader0.GetString()!);
                        break;
                    case "start":
                        reader0.Read();
                        start = roadNodeEndConverter.Read(ref reader0, typeof(RoadNodeEnd), options);
                        break;
                    case "end":
                        reader0.Read();
                        end = roadNodeEndConverter.Read(ref reader0, typeof(RoadNodeEnd), options);
                        break;
                    case "lanes":
                        JsonProcessor.ReadJsonArrayProperties(ref reader0, (ref reader1, _) => { 
                            var lane = laneStripConverter.Read(ref reader1, typeof(LaneStrip), options);
                            lanes.Add(lane);
                        });
                        break;
                }
            });

            if (start == null) throw new JsonException("Missing start property");
            if (end == null) throw new JsonException("Missing end property");

            var roadStrip = new RoadStrip(start, end);
            foreach (var lane in lanes) {
                roadStrip.AddLaneStrip(lane);
            }
            return roadStrip; 
            
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
