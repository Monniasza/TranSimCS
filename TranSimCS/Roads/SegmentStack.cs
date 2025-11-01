using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TranSimCS.Save2;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Stack;

namespace TranSimCS.Roads {
    public class SegmentStack : ObjectStack<RoadStrip, SegmentStack> {
        public readonly TrackerSpatial<RoadStrip, SegmentStack> trackerSpatial;
        public SegmentStack(TSWorld world) : base(world) {
            trackerSpatial = new TrackerSpatial<RoadStrip, SegmentStack>(world);
            stackTrackers.Add(trackerSpatial);
        }

        public override RoadStrip ReadElementFromJson(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            RoadNodeEnd? start = null;
            RoadNodeEnd? end = null;
            List<LaneStrip> lanes = new List<LaneStrip>();
            Guid? guid = Guid.Empty;
            RoadFinish finish = RoadFinish.Embankment;

            var roadNodeEndConverter = new RoadNodeEndConverter(World);
            var laneStripConverter = new LaneStripConverter(World);

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
                    case "finish":
                        var finishConverter = new RoadFinishConverter();
                        finish = finishConverter.Read(ref reader0, typeof(RoadFinish), options);
                        break;
                }
            });

            if (start == null) throw new JsonException("Missing start property");
            if (end == null) throw new JsonException("Missing end property");

            var roadStrip = new RoadStrip(start, end);
            roadStrip.Guid = guid ?? Guid.NewGuid();
            roadStrip.Finish = finish;

            foreach (var lane in lanes) {
                roadStrip.AddLaneStrip(lane);
            }
            return roadStrip;

        }

        public override void SaveElementToJson(Utf8JsonWriter writer, RoadStrip value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WriteString("guid", value.Guid.ToString());

            writer.WritePropertyName("start");
            var roadNodeEndConverter = new RoadNodeEndConverter(World);
            roadNodeEndConverter.Write(writer, value.StartNode, options);

            writer.WritePropertyName("end");
            roadNodeEndConverter.Write(writer, value.EndNode, options);

            writer.WritePropertyName("lanes");
            writer.WriteStartArray();
            var laneStripConverter = new LaneStripConverter(World);
            foreach (var lane in value.Lanes) {
                laneStripConverter.Write(writer, lane, options);
            }
            writer.WriteEndArray();

            var finishConverter = new RoadFinishConverter();
            writer.WritePropertyName("finish");
            finishConverter.Write(writer, value.Finish, options);

            writer.WriteEndObject();
        }
    }
}
