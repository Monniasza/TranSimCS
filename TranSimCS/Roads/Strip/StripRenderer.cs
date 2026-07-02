using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Setting;

namespace TranSimCS.Roads.Strip {
    public static class StripRenderer {

        public static void GenerateLaneStripMesh(LaneStrip laneStrip, MultiMesh renderer, float voffset = 0) {
            var accuracy = Settings.RoadAccuracy;
            var tag = laneStrip.Tag();
            var roadTag = laneStrip.road.FullSizeTag();
            var (Left, Right) = RoadRenderer.GenerateSplines(tag, voffset); // Generate the splines for the left and right lanes

            var apshaltBin = renderer.GetOrCreateRenderBinForced(Assets.Asphalt);
            var leftPoints = GeometryUtils.GenerateSplinePoints(Left, accuracy);
            var rightPoints = GeometryUtils.GenerateSplinePoints(Right, accuracy);
            var generatedVertStripPair = UniformTexturing.UniformTexturedTwin(leftPoints, rightPoints, GenerateLaneStripVertexGen(laneStrip.Spec));
            apshaltBin.DrawStrip(generatedVertStripPair);

            //Generate arrows
            float aoffset = 0.15f;
            var t = 0.5f;
            
            var avgspline = (Right + Left) / 2;
            var lpoint = Left[t];
            var rpoint = Right[t];
            var midpoint = (lpoint + rpoint) / 2;
            var tangent = avgspline.Tangential(t);
            
            tangent.Normalize();
            var fakebinormal = rpoint - lpoint;
            var width = Vector3.Cross(tangent, fakebinormal).Length();
            var normalfakebirnormal = Vector3.Normalize(fakebinormal);
            var nrm = Vector3.Cross(tangent, normalfakebirnormal);
            nrm.Normalize();

            var arrowWidth = width / 2;
            var displacement = tangent * width / 2;
            midpoint += nrm * aoffset;
            if (laneStrip.IsReverse()) displacement *= -1;

            var arrowBin = renderer.GetOrCreateRenderBinForced(Assets.Arrow);
            arrowBin.DrawLine(midpoint - displacement, midpoint + displacement, nrm, Color.White, arrowWidth);

            GenerateStripEdgeLines(laneStrip, renderer, voffset);

            renderer.AddTagsToAll(laneStrip);
        }
        public static void GenerateStripEdgeLines(LaneStrip laneStrip, MultiMesh renderer, float voffset = 0) {
            //Get side-line flags
            var mergeLeft = (laneStrip.Spec.Flags & LaneFlags.MergeLeft) != 0;
            var mergeRight = (laneStrip.Spec.Flags & LaneFlags.MergeRight) != 0;
            var isExpand = (laneStrip.Spec.Flags & LaneFlags.ExpandNotMerge) != 0;

            if (mergeLeft && mergeRight) mergeLeft = mergeRight = false;

            //Get tags
            var accuracy = Settings.RoadAccuracy;
            var tag = laneStrip.Tag();
            var roadTag = laneStrip.road.FullSizeTag();

            //Generate side-lines
            var solidTexture = Assets.White;
            var dashedTexture = ((laneStrip.Spec.Flags & LaneFlags.Yield) != 0) ? Assets.LineYield : Assets.LineDash;
            var lineWidth = laneStrip.Spec.LineWidth;

            void DrawSide(LaneRange laneRange, LaneFlags flag) {
                bool isEdge = IsRangeTouchingEdge(laneRange.startRange, roadTag.startRange) && IsRangeTouchingEdge(laneRange.endRange, roadTag.endRange);
                var lineSplines = RoadRenderer.GenerateSplines(laneRange, voffset + 0.05f);
                var lineTexture = ((laneStrip.Spec.Flags & flag) != 0 || isEdge) ? solidTexture : dashedTexture;
                var leftLinePoints = GeometryUtils.GenerateSplinePoints(lineSplines.Left, accuracy);
                var rightLinePoints = GeometryUtils.GenerateSplinePoints(lineSplines.Right, accuracy);
                var generatedVertStripPair = UniformTexturing.UniformTexturedTwin(leftLinePoints, rightLinePoints, GenerateLaneStripVertexGen(Color.White));
                var lineBin = renderer.GetOrCreateRenderBinForced(lineTexture);
                lineBin.DrawStrip(generatedVertStripPair);
            }
            bool IsRangeTouchingEdge(Range<float> lineWidth, Range<float> endingRange) {
                float delta = 0.01f;
                var d0 = Math.Abs(lineWidth.Min - endingRange.Min);
                var d1 = Math.Abs(lineWidth.Max - endingRange.Max);
                return (d0 < delta) || (d1 < delta);
            }

            var startLeft = tag.startRange.Min;
            var startLeftCenter = tag.startRange.Min + lineWidth;
            var startRightCenter = tag.startRange.Max - lineWidth;
            var startRight = tag.startRange.Max;
            if (laneStrip.StartLane.end == Node.NodeEnd.Backward) {
                DataUtil.Swap(ref startLeft, ref startRightCenter);
                DataUtil.Swap(ref startLeftCenter, ref startRight);
            }
            var endLeft = tag.endRange.Min;
            var endLeftCenter = tag.endRange.Min + lineWidth;
            var endRightCenter = tag.endRange.Max - lineWidth;
            var endRight = tag.endRange.Max;
            if (laneStrip.EndLane.end == Node.NodeEnd.Backward) {
                DataUtil.Swap(ref endLeft, ref endRightCenter);
                DataUtil.Swap(ref endLeftCenter, ref endRight);
            }

            var leftRange = tag;
            leftRange.startRange = new(startLeft, startLeftCenter);
            leftRange.endRange = new(endRightCenter, endRight);
            var rightRange = tag;
            rightRange.startRange = new(startRightCenter, startRight);
            rightRange.endRange = new(endLeft, endLeftCenter);

            //Do merges
            if (isExpand) {
                //Work on the end
                var endLeft1 = (endLeft, endLeftCenter);
                var endRight1 = (endRight, endRightCenter);
                var endLeft2 = mergeLeft ? endRight1 : endLeft1;
                var endRight2 = mergeRight ? endLeft1 : endRight1;
                endLeft = endLeft2.Item1;
                endLeftCenter = endLeft2.Item2;
                endRightCenter = endRight2.Item1;
                endRight = endRight2.Item2;
            } else {
                //Work on the start
                var startLeft1 = (startLeft, startLeftCenter);
                var startRight1 = (startRight, startRightCenter);
                var startLeft2 = mergeLeft ? startRight1 : startLeft1;
                var startRight2 = mergeRight ? startLeft1 : startRight1;
                startLeft = startLeft2.Item1;
                startLeftCenter = startLeft2.Item2;
                startRightCenter = startRight2.Item1;
                startRight = startRight2.Item2;
            }

            DrawSide(leftRange, LaneFlags.NoLeft);
            DrawSide(rightRange, LaneFlags.NoRight);
        }

        public static VertexGen2<VertexPositionColorTexture> GenerateLaneStripVertexGen(LaneSpec spec) => GenerateLaneStripVertexGen(spec.Color);
        public static VertexGen2<VertexPositionColorTexture> GenerateLaneStripVertexGen(Color c) {
            (VertexPositionColorTexture, VertexPositionColorTexture) GenerateVertices(Vector3 l, Vector3 r, float distance, int index) {
                float mutualDistance = Vector3.Distance(l, r) / 2;
                return (
                    new VertexPositionColorTexture(l, c, new(-mutualDistance, distance)),
                    new VertexPositionColorTexture(r, c, new(mutualDistance, distance))
                );
            }
            return GenerateVertices;
        }
    }
}