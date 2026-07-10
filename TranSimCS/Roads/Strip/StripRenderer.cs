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
            if (tangent.LengthSquared() < 0.0000001) return; //Zero tangential. It's wrong!
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
            var isMerge = (laneStrip.Spec.Flags & LaneFlags.IsMerge) != 0;

            if (mergeLeft && mergeRight) return;
            var swapMerges = isMerge ? laneStrip.EndLane.end == Node.NodeEnd.Backward : laneStrip.StartLane.end == Node.NodeEnd.Backward;
            if (swapMerges) DataUtil.Swap(ref mergeLeft, ref mergeRight);

            //Get tags
            var accuracy = Settings.RoadAccuracy;
            var tag = laneStrip.Tag();
            var roadTag = laneStrip.road.FullSizeTag();

            //Generate side-lines
            var solidTexture = Assets.EmissiveWhite;
            var dashedTexture = ((laneStrip.Spec.Flags & LaneFlags.Yield) != 0) ? Assets.LineYield : Assets.LineDash;
            var lineWidth = laneStrip.Spec.LineWidth;

            void DrawSide(LaneRange laneRange, LaneFlags flag, float bias) {
                bool isEdge = IsRangeTouchingEdge(laneRange.startRange, roadTag.startRange) && IsRangeTouchingEdge(laneRange.endRange, roadTag.endRange);
                var lineSplines = RoadRenderer.GenerateSplines(laneRange, voffset + 0.05f);
                var lineTexture = ((laneStrip.Spec.Flags & flag) != 0 || isEdge) ? solidTexture : dashedTexture;
                var leftLinePoints = GeometryUtils.GenerateSplinePoints(lineSplines.Left, accuracy);
                var rightLinePoints = GeometryUtils.GenerateSplinePoints(lineSplines.Right, accuracy);
                var generatedVertStripPair = UniformTexturing.UniformTexturedTwin(leftLinePoints, rightLinePoints, GenerateLaneStripVertexGen(Color.White), bias);
                var lineBin = renderer.GetOrCreateRenderBinForced(lineTexture);
                lineBin.DrawStrip(generatedVertStripPair);
            }
            bool IsRangeTouchingEdge(Range<float> lineWidth, Range<float> endingRange) {
                float delta = 0.01f;
                var d0 = Math.Abs(lineWidth.Min - endingRange.Min);
                var d1 = Math.Abs(lineWidth.Max - endingRange.Max);
                return (d0 < delta) || (d1 < delta);
            }

            var startRange = laneStrip.StartLane.Range();
            var endRange = laneStrip.EndLane.Range();
            
            var startLeft = startRange.Min;
            var startRight = startRange.Max;
            var endLeft = endRange.Min;  
            var endRight = endRange.Max;
            var linewidth = laneStrip.Spec.LineWidth;

            //Do merges
            if (isMerge) {
                //Merge the end
                if (mergeLeft) endRight = endLeft + linewidth;
                if (mergeRight) endLeft = endRight - linewidth;
            } else {
                //Merge the start
                if (mergeLeft) startRight = startLeft + linewidth;
                if (mergeRight) startLeft = startRight - linewidth;
            }

            var startLeftCenter = startLeft + lineWidth;
            var startRightCenter = startRight - lineWidth;
            var endLeftCenter = endLeft + lineWidth;
            var endRightCenter = endRight - lineWidth;

            //Do ordering
            if (laneStrip.StartLane.end == Node.NodeEnd.Backward) {
                DataUtil.Swap(ref startLeft, ref startRightCenter);
                DataUtil.Swap(ref startLeftCenter, ref startRight);
            }

            if (laneStrip.EndLane.end == Node.NodeEnd.Forward) {
                DataUtil.Swap(ref endLeft, ref endRightCenter);
                DataUtil.Swap(ref endLeftCenter, ref endRight);
            }

            var leftRange = LaneStripToRoadStripRange(laneStrip, new(startLeft, startLeftCenter), new(endLeft, endLeftCenter));
            var rightRange = LaneStripToRoadStripRange(laneStrip, new(startRightCenter, startRight), new(endRightCenter, endRight));  
            DrawSide(leftRange, LaneFlags.NoLeft, 0);
            DrawSide(rightRange, LaneFlags.NoRight, 1);
        }

        public static LaneRange LaneStripToRoadStripRange(LaneStrip strip, Range<float> startRange, Range<float> endRange) {
            if (strip.IsReverse()) DataUtil.Swap(ref startRange, ref endRange);
            return new(strip.road, startRange, endRange);
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