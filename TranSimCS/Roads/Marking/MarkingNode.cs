using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Ui;
using MonoGame.Extended;
using TranSimCS.Model;
using TranSimCS.Roads.Node;
using TranSimCS.Save2;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Marking {
    public struct MarkingPointData {
        public IRoadElement? Anchor;
        public float Alignment;
        public float Offset;
    }

    public class MarkingPointDataConverter(TSWorld world) : JsonConverter<MarkingPointData> {
        private readonly RoadNodeEndConverter endConverter = new(world);
        public override MarkingPointData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            MarkingPointData data = default;
            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, key) => {
                switch (key.ToLower()) {
                    case "anchorNode":
                        data.Anchor = endConverter.Read(ref reader0, typeof(RoadNodeEnd), options);
                        break;
                    case "anchorLane":
                        if (data.Anchor == null) throw new JsonException("anchorLane needs anchorNode first");
                        if (data.Anchor is LaneEnd) throw new JsonException("anchorLane already set");
                        var index = reader0.GetInt32();
                        var laneEnd = data.Anchor.GetRoadNode().Lanes[index];
                        break;
                    case "alignment":
                        data.Alignment = reader0.GetSingle();
                        break;
                    case "offset":
                        data.Offset = reader0.GetSingle();
                        break;
                }
            });
            return data;
        }

        public override void Write(Utf8JsonWriter writer, MarkingPointData value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            if (value.Anchor != null) {
                writer.WritePropertyName("anchorNode");
                endConverter.Write(writer, value.Anchor.GetNodeEnd(), options);
            }
            if (value.Anchor is LaneEnd le) {
                writer.WritePropertyName("anchorLane");
                writer.WriteNumberValue(le.lane.Index);
            }
            writer.WritePropertyName("alignment");
            writer.WriteNumberValue(value.Alignment);
            writer.WritePropertyName("offset");
            writer.WriteNumberValue(value.Offset);
            writer.WriteEndObject();
        }
    }

    public class MarkingRenderer {
        public static void RenderMarkingPoint(MarkingPointData marking, MultiMesh mesh) {
            var anchor = marking.Anchor;
            float leftBound = 0;
            float rightBound = 0;
            var alignment = marking.Alignment;
            var offset = marking.Offset;
            ObjPos refpos = default;
            LaneEnd? laneEnd = anchor.GetLaneEnd();
            RoadNodeEnd nodeEnd = anchor.GetNodeEnd();
            if(nodeEnd != null) refpos = nodeEnd.PositionProp.Value;
            if (laneEnd != null) {
                leftBound = laneEnd.Value.lane.LeftPosition;
                rightBound = laneEnd.Value.lane.RightPosition;
                if (laneEnd.Value.end == NodeEnd.Backward)
                    (leftBound, rightBound) = (rightBound, leftBound);
            } else if (nodeEnd != null) {
                var bounds = nodeEnd.Bounds();
                leftBound = bounds.X;
                rightBound = bounds.Y;
            }

            var alignedPos = float.Lerp(leftBound, rightBound, alignment) + offset;

            var refframe = refpos.CalcReferenceFrame();

            var sideLen = 0.2f;

            var centerPos = refframe.O + refframe.X * alignedPos;
            var ulpos = centerPos - (refframe.Z - refframe.X) * (sideLen * 0.5f);

            var arrowRenderBin = mesh.GetOrCreateRenderBinForced(Assets.Arrow);
            arrowRenderBin.DrawParallelogram(ulpos, refframe.X * sideLen, -refframe.Z * sideLen, Color.Yellow);
        }
    }
}
