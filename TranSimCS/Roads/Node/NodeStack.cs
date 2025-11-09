using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TranSimCS.Save2;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Stack;

namespace TranSimCS.Roads.Node {
    /// <summary>
    /// A collection of <see cref="RoadNode"/>s in a <see cref="TSWorld"/>
    /// </summary>
    public class NodeStack : ObjectStack<RoadNode, NodeStack> {
        public readonly TrackerSpatial<RoadNode, NodeStack> trackerSpatial;
        public NodeStack(TSWorld world) : base(world) {
            trackerSpatial = new TrackerSpatial<RoadNode, NodeStack> (world);
            stackTrackers.Add(trackerSpatial);
        }
        public override RoadNode ReadElementFromJson(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            Guid? guid = null;
            ObjPos? pos = null;
            List<Lane> lanes = new List<Lane>();
            string name = "";

            var objPosConverter = new ObjPosConverter();
            var laneConverter = new LaneConverter();

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "id":
                        reader0.Read();
                        guid = Guid.Parse(reader0.GetString()!);
                        break;
                    case "pos":
                        pos = objPosConverter.Read(ref reader0, typeof(ObjPos), options);
                        break;
                    case "lanes":
                        JsonProcessor.ReadJsonArrayProperties(ref reader0, (ref reader1, _) => {
                            var lane = laneConverter.Read(ref reader1, typeof(Lane), options);
                            lanes.Add(lane);
                        });
                        break;
                    case "name":
                        reader0.Read();
                        name = reader0.GetString() ?? "";
                        break;
                }
            });

            if (guid == null) throw new JsonException("Missing id property");
            if (pos == null) throw new JsonException($"Missing pos property for node {guid}");

            RoadNode node = new RoadNode(World, name, pos.Value, guid);
            foreach (var lane in lanes) {
                node.AddLane(lane);
            }
            return node;
        }

        public override void SaveElementToJson(Utf8JsonWriter writer, RoadNode value, JsonSerializerOptions options) {
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
