using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public readonly ReadOnlyDictionary<Guid, Lane> LaneXRef;
        internal readonly Dictionary<Guid, Lane> laneXRef;
        public NodeStack(TSWorld world) : base(world) {
            trackerSpatial = new TrackerSpatial<RoadNode, NodeStack> (world);
            stackTrackers.Add(trackerSpatial);
            laneXRef = new();
            LaneXRef = new(laneXRef);
            data.ItemAdded += Data_ItemAdded;
            data.ItemRemoved += Data_ItemRemoved;
        }

        private void Data_ItemRemoved(RoadNode obj) {
            //Delete lanes from xref
            foreach (var lane in obj.Lanes) laneXRef.Remove(lane.Guid, out var _);

            //Un-listen to lane changes
            obj.LaneAdded += Obj_LaneAdded;
            obj.LaneRemoved += Obj_LaneRemoved;
        }
        private void Data_ItemAdded(RoadNode obj) {
            //Add lanes to xref
            foreach(var lane in obj.Lanes) laneXRef[lane.Guid] = lane;

            //Listen to lane changes
            obj.LaneAdded += Obj_LaneAdded;
            obj.LaneRemoved += Obj_LaneRemoved;
        }

        private void Obj_LaneRemoved(object? sender, RoadNode.LaneEventArgs e) {
            laneXRef.Remove(e.lane.Guid, out var _);
        }

        private void Obj_LaneAdded(object? sender, RoadNode.LaneEventArgs e) {
            laneXRef[e.lane.Guid] = e.lane;
        }

        public override RoadNode ReadElementFromJson(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            Guid? guid = null;
            PositionEulerAngles? pos = null;
            List<LaneNode> lanes = new();
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
                        pos = objPosConverter.Read(ref reader0, typeof(PositionEulerAngles), options);
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

            RoadNode node = new RoadNode(name, pos.Value, guid);
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
                laneConverter.Write(writer, lane.Definition, options);
            }
            writer.WriteEndArray();

            if (!string.IsNullOrEmpty(value.Name)) {
                writer.WriteString("name", value.Name);
            }

            writer.WriteEndObject();
        }
    }
}
