using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads.Range;
using TranSimCS.Setting;

namespace TranSimCS.Roads.Strip {
    public static class StripRenderer {

        public delegate void StripBoundsGenerator(LaneStripData laneStrip, Action<RoadSplineComponent, RoadSplineRange> pushResults);
        public static event StripBoundsGenerator OnLaneStripGenerated;
        public static (RoadSplineComponent, RoadSplineRange)[] GenerateStripSplineComponents(LaneStripData laneStrip) {
            List<(RoadSplineComponent, RoadSplineRange)> generatedRanges = new();
            void AddComponent(RoadSplineComponent component, RoadSplineRange laneRange) => generatedRanges.Add((component, laneRange));
            OnLaneStripGenerated?.Invoke(laneStrip, AddComponent);
            return generatedRanges.ToArray();
        }
        static StripRenderer() {
            OnLaneStripGenerated += GenerateStripAllComponents;
        }

        public static void GenerateLaneStripMesh(LaneStripData laneStrip, MultiMesh renderer, float voffset = 0) {
            //Generate arrows
            var bounds = laneStrip.Bounds;
            var averageStripWidth = (bounds.endRange.Max + bounds.startRange.Max - bounds.startRange.Min - bounds.endRange.Min)/2;
            var basis = laneStrip.Parent.OrthodistantBasis.Offset(bounds.startRange.Middle(), -bounds.endRange.Middle());

            float aoffset = 0.15f;
            var centerframe = basis.SampleFrame(0.5f);
            var binormal = centerframe.X;
            var midpoint = centerframe.O;
            var tangent = centerframe.Z;
            var nrm = centerframe.Y;
            if (tangent.LengthSquared() >= 0.0000001){
                tangent.Normalize();
                nrm.Normalize();

                var arrowWidth = averageStripWidth / 2;
                var displacement = tangent * averageStripWidth / 2;
                midpoint += nrm * aoffset;
                if (laneStrip.IsReverse) displacement *= -1;

                var arrowBin = renderer.GetOrCreateRenderBinForced(Assets.Arrow);
                arrowBin.DrawLine(midpoint - displacement, midpoint + displacement, nrm, Color.White, arrowWidth);
            } //else Zero tangential. It's wrong!

            //Generate strips themselves
            var gridmesh = laneStrip.AllStrips;
            Vector3[][] arrays = new Vector3[gridmesh.Vertices.Width()][];
            for (int i = 0; i < gridmesh.Vertices.Width(); i++) {
                var array = new Vector3[gridmesh.Vertices.Height()];
                for (int j = 0; j < array.Length; j++) array[j] = gridmesh.Vertices[i,j];
                arrays[i] = array;
            }
            foreach (var line in gridmesh.CrossSections) {
                var mat = line.Value.Type.GetMaterial();
                if(mat == null) continue;
                var leftLinePoints = arrays[line.MinIndex];
                var rightLinePoints = arrays[line.MaxIndex];
                var generatedLineVertStripPair = UniformTexturing.UniformTexturedTwin(leftLinePoints, rightLinePoints, TranSimCS.Model.UniformTexturing.GenerateLaneStripVertexGen(line.Value.Color), line.Value.Bias);             
                var lineBin = renderer.GetOrCreateRenderBinForced(mat.Value);
                lineBin.DrawStrip(generatedLineVertStripPair);
            }

            renderer.AddTagsToAll(laneStrip.Tag);
        }

        public static void GenerateStripAllComponents(LaneStripData strip, Action<RoadSplineComponent, RoadSplineRange> target) {
            GenerateStripEdgeLines(strip, target, 0.15f);

            //Generate the asphalt
            var (asphaltSplineComponent, asphaltRange) = GenerateAsphaltStrip(strip);
            target(asphaltSplineComponent, asphaltRange);

            //Generate the clip
            var (drivableSplineComponent, drivableRange) = GenerateDrivableCache(strip);
            target(drivableSplineComponent, drivableRange);
        }

        private static (RoadSplineComponent splineComponent, RoadSplineRange range) GenerateDrivableCache(LaneStripData strip) {
            var linewidth = strip.Spec.LineWidth;
            var tag = strip.Bounds;
            var startl = tag.startRange.Min + linewidth;
            var endl = tag.endRange.Min + linewidth;
            var startr = tag.startRange.Max - linewidth;
            var endr = tag.endRange.Max - linewidth;
            if (endl > endr) endl = endr = (endl + endr) / 2;
            if (startl > startr) startl = startr = (startr + startl) / 2;
            tag.startRange = new(startl, startr);
            tag.endRange = new(endl, endr);
            var splineComponent = new RoadSplineComponent() {
                Bias = 0.5f,
                Color = Color.Transparent,
                Type = RoadSplineComponentType.DrivingAreaMarker
            };
            var splineRange = tag.ToRoadSplineRange();
            return (splineComponent, splineRange);
        }
        private static (RoadSplineComponent splineComponent, RoadSplineRange range) GenerateAsphaltStrip(LaneStripData strip) {
            var splineComponent = new RoadSplineComponent() {
                Bias = 0.5f,
                Color = strip.Spec.Color,
                Type = RoadSplineComponentType.Asphalt
            };
            var range = strip.Bounds.ToRoadSplineRange();
            return (splineComponent, range);
        }

        public static void GenerateStripEdgeLines(LaneStripData laneStrip, Action<RoadSplineComponent, RoadSplineRange> target, float voffset = 0) {
            //Get side-line flags
            var mergeLeft = (laneStrip.Spec.Flags & LaneFlags.MergeLeft) != 0;
            var mergeRight = (laneStrip.Spec.Flags & LaneFlags.MergeRight) != 0;
            var isMerge = (laneStrip.Spec.Flags & LaneFlags.IsMerge) != 0;

            if (mergeLeft && mergeRight) return;

            //Get tags
            var roadTag = laneStrip.Parent.Bounds;

            //Generate side-lines
            var lineWidth = laneStrip.Spec.LineWidth;

            RoadSplineComponent DrawSide(DualRange laneRange, LaneFlags flag, float bias) {
                bool isEdge = IsRangeTouchingEdge(laneRange.startRange, roadTag.startRange) && IsRangeTouchingEdge(laneRange.endRange, roadTag.endRange);
                var lineTexture = ((laneStrip.Spec.Flags & flag) != 0 || isEdge) ? RoadSplineComponentType.Solid : RoadSplineComponentType.Dashed;
                return new RoadSplineComponent() {
                    Bias = bias,
                    Color = Color.White,
                    Type = lineTexture
                };
            }

            bool IsRangeTouchingEdge(Range<float> lineWidth, Range<float> endingRange) {
                float delta = 0.01f;
                var d0 = Math.Abs(lineWidth.Min - endingRange.Min);
                var d1 = Math.Abs(lineWidth.Max - endingRange.Max);
                return (d0 < delta) || (d1 < delta);
            }

            var startRange = laneStrip.StartNode.Bounds;
            var endRange = laneStrip.EndNode.Bounds;
            
            var startLeft = startRange.Min;
            var startRight = startRange.Max;
            var endLeft = endRange.Max;  
            var endRight = endRange.Min;
            var linewidth = laneStrip.Spec.LineWidth;

            //Do merges
            if (isMerge) {
                //Merge the end
                if (mergeLeft) endLeft = endRight + linewidth;
                if (mergeRight) endRight = endLeft - linewidth;
            } else {
                //Merge the start
                if (mergeLeft) startRight = startLeft + linewidth;
                if (mergeRight) startLeft = startRight - linewidth;
            }

            var startLeftCenter = startLeft + lineWidth;
            var startRightCenter = startRight - lineWidth;
            var endLeftCenter = endLeft - lineWidth;
            var endRightCenter = endRight + lineWidth;

            var leftRange = LaneStripToRoadStripRange(laneStrip, new(startLeft, startLeftCenter), new(endLeftCenter, endLeft));
            var rightRange = LaneStripToRoadStripRange(laneStrip, new(startRightCenter, startRight), new(endRight, endRightCenter));
            target(DrawSide(leftRange, LaneFlags.NoLeft, 0), leftRange.ToRoadSplineRange(voffset));
            target(DrawSide(rightRange, LaneFlags.NoRight, 1), rightRange.ToRoadSplineRange(voffset));
        }

        
        public static Vector3 VOffset(float x, float y) => new(x, y, 0);
        public static DualRange LaneStripToRoadStripRange(LaneStripData strip, Range<float> startRange, Range<float> endRange) {
            if (strip.IsReverse) DataUtil.Swap(ref startRange, ref endRange);
            return new(startRange, endRange);
        }
    }
}