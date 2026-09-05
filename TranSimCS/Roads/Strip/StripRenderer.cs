using System;
using System.Collections.Generic;
using System.Numerics;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads.Range;

namespace TranSimCS.Roads.Strip {
    public static class StripRenderer {

        public delegate void StripBoundsGenerator(LaneStrip laneStrip, Action<RoadSplineComponent, RoadSplineRange> pushResults);
        public static event StripBoundsGenerator OnLaneStripGenerated;
        public static (RoadSplineComponent, RoadSplineRange)[] GenerateStripSplineComponents(LaneStrip laneStrip) {
            List<(RoadSplineComponent, RoadSplineRange)> generatedRanges = new();
            void AddComponent(RoadSplineComponent component, RoadSplineRange laneRange) => generatedRanges.Add((component, laneRange));
            OnLaneStripGenerated?.Invoke(laneStrip, AddComponent);
            return generatedRanges.ToArray();
        }
        static StripRenderer() {
            OnLaneStripGenerated += GenerateStripAllComponents;
        }

        public static void GenerateLaneStripMesh(LaneStrip laneStrip, MultiMesh renderer, float voffset = 0) {
            //Generate arrows
            var bounds = laneStrip.Bounds;
            var averageStripWidth = (bounds.endRange.Max + bounds.startRange.Max - bounds.startRange.Min - bounds.endRange.Min)/2;
            var basis = laneStrip.Road.OrthodistantBasis.Offset(bounds.startRange.Middle(), -bounds.endRange.Middle());

            float aoffset = 0.15f;
            var centerframe = basis.SampleFrame(0.5f);
            var midpoint = centerframe.O;
            var tangent = centerframe.Z;
            var nrm = centerframe.Y;
            bool removeArrows = laneStrip.LaneSpec.Flags.HasFlags(LaneFlags.Sidewalk | LaneFlags.Platform);
            if (!removeArrows && tangent.LengthSquared() >= 0.000001){
                tangent = tangent.Normalized();
                nrm = nrm.Normalized();

                var arrowWidth = averageStripWidth / 2;
                var displacement = tangent * averageStripWidth / 2;
                midpoint += nrm * aoffset;
                if (laneStrip.IsReverse()) displacement *= -1;

                var arrowColor = Colors.White;
                //Show direction by switchin to light yellow if reverse
                if (laneStrip.IsReverse()) arrowColor = Colors.LightYellow;

                var arrowBin = renderer.GetOrCreateRenderBinForced(Materials.Arrow);
                arrowBin.DrawLine(midpoint - displacement, midpoint + displacement, nrm, arrowColor, arrowWidth);
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
                var mat = line.Value.Texture;
                if(mat == null) continue;
                var leftLinePoints = arrays[line.MinIndex];
                var rightLinePoints = arrays[line.MaxIndex];
                var generatedLineVertStripPair = UniformTexturing.UniformTexturedTwin(leftLinePoints, rightLinePoints, UniformTexturing.GenerateLaneStripVertexGen(line.Value.Color), line.Value.Bias);             
                var lineBin = renderer.GetOrCreateRenderBinForced(mat.Value);
                lineBin.DrawStrip(generatedLineVertStripPair);
            }

            renderer.AddTagsToAll(laneStrip);
        }

        public static void GenerateStripAllComponents(LaneStrip strip, Action<RoadSplineComponent, RoadSplineRange> target) {
            GenerateStripEdgeLines(strip, target, 0.05f);

            //Generate the asphalt
            var (asphaltSplineComponent, asphaltRange) = GenerateAsphaltStrip(strip);
            target(asphaltSplineComponent, asphaltRange);

            //Generate the clip
            var (drivableSplineComponent, drivableRange) = GenerateDrivableCache(strip);
            target(drivableSplineComponent, drivableRange);
        }

        private static (RoadSplineComponent splineComponent, RoadSplineRange range) GenerateDrivableCache(LaneStrip strip) {
            var linewidth = strip.LaneSpec.LineWidth;
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
                Color = Colors.Transparent,
                Type = RoadSplineComponentType.MarkingClip,
                Texture = null
            };
            var splineRange = tag.ToRoadSplineRange();
            return (splineComponent, splineRange);
        }
        private static (RoadSplineComponent splineComponent, RoadSplineRange range) GenerateAsphaltStrip(LaneStrip strip) {
            var splineComponent = new RoadSplineComponent() {
                Bias = 0.5f,
                Color = strip.LaneSpec.Color,
                Type = RoadSplineComponentType.RoadSurface,
                Texture = strip.LaneSpec.Surface.GetTexture(),
            };
            var range = strip.Bounds.ToRoadSplineRange();
            return (splineComponent, range);
        }

        public static void GenerateStripEdgeLines(LaneStrip laneStrip, Action<RoadSplineComponent, RoadSplineRange> target, float voffset = 0) {
            var color = Colors.White;

            //Get side-line flags
            var mergeLeft = (laneStrip.LaneSpec.Flags & LaneFlags.MergeLeft) != 0;
            var mergeRight = (laneStrip.LaneSpec.Flags & LaneFlags.MergeRight) != 0;
            var isMerge = (laneStrip.LaneSpec.Flags & LaneFlags.IsMerge) != 0;

            if (mergeLeft && mergeRight) return;
            if (laneStrip.LaneSpec.Flags.HasFlags(LaneFlags.Sidewalk)) return;
            if (laneStrip.LaneSpec.Flags.HasFlags(LaneFlags.Platform)) color = Colors.Yellow;

            //Get tags
            var roadTag = laneStrip.Road.Bounds;

            //Generate side-lines
            var lineWidth = laneStrip.LaneSpec.LineWidth;
            if (lineWidth <= 0) return;

            RoadSplineComponent DrawSide(DualRange laneRange, LaneFlags flag, float bias) {
                bool isSolid = IsRangeTouchingEdge(laneRange.startRange, roadTag.startRange) && IsRangeTouchingEdge(laneRange.endRange, roadTag.endRange);
                var leftEdges = laneStrip.Road.Extents.LeftEdgeStrips;
                var rightEdges = laneStrip.Road.Extents.RightEdgeStrips;
                if(laneStrip.IsReverse()) DataUtil.Swap(ref leftEdges, ref rightEdges);
                bool isOnLeftExtent = (flag & LaneFlags.NoLeft) != 0 && leftEdges.Contains(laneStrip);
                bool isOnRightExtent = (flag & LaneFlags.NoRight) != 0 && rightEdges.Contains(laneStrip);
                bool isPlatform = laneStrip.LaneSpec.Flags.HasFlags(LaneFlags.Platform);

                isSolid |= (laneStrip.LaneSpec.Flags & flag) != 0 || isOnLeftExtent || isOnRightExtent || isPlatform;
                var lineTexture = isSolid ? RoadSplineComponentType.ClippedMarking : RoadSplineComponentType.UnclippedMarking;
                return new RoadSplineComponent() {
                    Bias = bias,
                    Color = color,
                    Type = lineTexture,
                    Texture = isSolid ? Materials.EmissiveWhite : Materials.LineDash,
                };
            }

            bool IsRangeTouchingEdge(Interval<float> lineWidth, Interval<float> endingRange) {
                float delta = 0.01f;
                var d0 = Math.Abs(lineWidth.Min - endingRange.Min);
                var d1 = Math.Abs(lineWidth.Max - endingRange.Max);
                return (d0 < delta) || (d1 < delta);
            }

            var startRange = laneStrip.StartLane.Bounds;
            var endRange = laneStrip.EndLane.Bounds;
            
            var startLeft = startRange.Min;
            var startRight = startRange.Max;
            var endLeft = endRange.Max;  
            var endRight = endRange.Min;
            var linewidth = laneStrip.LaneSpec.LineWidth;

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
        public static DualRange LaneStripToRoadStripRange(LaneStrip strip, Interval<float> startRange, Interval<float> endRange) {
            if (strip.IsReverse()) DataUtil.Swap(ref startRange, ref endRange);
            return new(startRange, endRange);
        }
    }
}