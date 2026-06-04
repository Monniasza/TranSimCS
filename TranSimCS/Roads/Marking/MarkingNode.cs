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
                        if (data.Anchor is LaneEnd) throw new JsonException("anchorLane already set");
                        if (data.Anchor == null) {
                            LaneEndConverter lec = new(world);
                            data.Anchor = lec.Read(ref reader0, typeof(LaneEnd), options);
                        } else {
                            var index = reader0.GetInt32();
                            var laneEnd = data.Anchor.GetRoadNode().SortedLanes[index];
                        }                            
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
        public static void RenderMarkingPoint(MarkingPointData marking, MultiMesh mesh, float voffset = 0.1f) {
            var anchor = marking.Anchor;
            float leftBound = 0;
            float rightBound = 0;
            var alignment = marking.Alignment;
            var offset = marking.Offset;
            ObjPos refpos = ObjPos.Zero;
            LaneEnd? laneEnd = anchor?.GetLaneEnd();
            RoadNodeEnd? nodeEnd = anchor?.GetNodeEnd();
            if(nodeEnd != null) refpos = nodeEnd.PositionProp.Value;
            if (laneEnd != null) {
                (_, _, leftBound, rightBound) = laneEnd.Value.Boundaries();
            } else if (nodeEnd != null) {
                (_, _, leftBound, rightBound) = nodeEnd.Bounds();
            }

            var alignedPos = float.Lerp(leftBound, rightBound, alignment) + offset;

            var refframe = refpos.CalcReferenceFrame();
            if (anchor != null && anchor.ZDiscriminant() < 0) {
                float width = Math.Abs(rightBound - leftBound);
                var roadNode = anchor.GetRoadNode();
                if (roadNode == null || roadNode.Lanes.Count == 0) return;
                var originalBounds = anchor.GetRoadNode().Bounds;
                float originalRightBound = originalBounds.Max;
                float originalLeftBound = originalBounds.Min;
                refframe.O += refframe.X * (originalRightBound + originalLeftBound);
                refframe.X *= -1;
                refframe.Z *= -1;
            }

            var sideLen = 0.6f;

            var centerPos = refframe.O + refframe.X * alignedPos + refframe.Y * voffset;
            var sidevector = refframe.X * sideLen * 0.5f;
            var fwdVector = refframe.Z * sideLen;

            var arrowRenderBin = mesh.GetOrCreateRenderBinForced(Assets.Arrow);
            arrowRenderBin.DrawQuad(
                centerPos - sidevector + fwdVector, centerPos + sidevector + fwdVector,
                centerPos + sidevector, centerPos - sidevector,
                Color.Magenta
            );
            arrowRenderBin.AddTagsToLastTriangles(2, marking);
        }
    }
}
