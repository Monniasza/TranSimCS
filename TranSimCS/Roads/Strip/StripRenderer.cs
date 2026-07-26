using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Setting;

namespace TranSimCS.Roads.Strip {
    public static class StripRenderer {
        //Lane strip generator listeners
        /// <summary>
        /// A range for road spline components. Must follow half-node convention.
        /// </summary>
        public struct RoadSplineRange {
            public Vector3 startLeft;
            public Vector3 endLeft;
            public Vector3 startRight;
            public Vector3 endRight;

            public RoadSplineRange(Vector3 startLeft, Vector3 endLeft, Vector3 startRight, Vector3 endRight) {
                this.startLeft = startLeft;
                this.endLeft = endLeft;
                this.startRight = startRight;
                this.endRight = endRight;
            }

        }
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
            var accuracy = Settings.RoadAccuracy;
            var tag = laneStrip.Tag();
            var roadTag = laneStrip.Road.Bounds;
            var (Left, Right) = RoadRenderer.GenerateSplines(tag, voffset); // Generate the splines for the left and right lanes

            //Generate arrows
            var averageStripWidth = (tag.endRange.Max + tag.startRange.Max - tag.startRange.Min - tag.endRange.Min)/2;

            float aoffset = 0.15f;
            var centerframe = laneStrip.Road.OrthodistantBasis.Sample(0.5f);
            var binormal = centerframe.X;
            var midpoint = centerframe.O;
            var tangent = centerframe.Z;
            var nrm = centerframe.Y;
            if (tangent.LengthSquared() >= 0.0000001){
                tangent.Normalize();
                var fakebinormal = binormal;
                var width = Vector3.Cross(tangent, fakebinormal).Length() * averageStripWidth;
                var normalfakebirnormal = Vector3.Normalize(fakebinormal);
                nrm.Normalize();

                var arrowWidth = width / 2;
                var displacement = tangent * width / 2;
                midpoint += nrm * aoffset;
                if (laneStrip.IsReverse()) displacement *= -1;

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
                var generatedLineVertStripPair = UniformTexturing.UniformTexturedTwin(leftLinePoints, rightLinePoints, GenerateLaneStripVertexGen(line.Value.Color), line.Value.Bias);             
                var lineBin = renderer.GetOrCreateRenderBinForced(mat.Value);
                lineBin.DrawStrip(generatedLineVertStripPair);
            }

            renderer.AddTagsToAll(laneStrip);
        }

        public static void GenerateStripAllComponents(LaneStrip strip, Action<RoadSplineComponent, RoadSplineRange> target) {
            GenerateStripEdgeLines(strip, target, 0.15f);

            //Generate the asphalt
            var (asphaltSplineComponent, asphaltRange) = GenerateAsphaltStrip(strip);
            target(asphaltSplineComponent, asphaltRange);

            //Generate the clip
            var (drivableSplineComponent, drivableRange) = GenerateDrivableCache(strip);
            target(drivableSplineComponent, drivableRange);
        }

        private static (RoadSplineComponent splineComponent, RoadSplineRange range) GenerateDrivableCache(LaneStrip strip) {
            var linewidth = strip.Spec.LineWidth;
            var tag = strip.Tag();
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
            var splineRange = ToRoadSplineRange(tag);
            return (splineComponent, splineRange);
        }
        private static (RoadSplineComponent splineComponent, RoadSplineRange range) GenerateAsphaltStrip(LaneStrip strip) {
            var splineComponent = new RoadSplineComponent() {
                Bias = 0.5f,
                Color = strip.Spec.Color,
                Type = RoadSplineComponentType.Asphalt
            };
            var range = ToRoadSplineRange(strip.Tag());
            return (splineComponent, range);
        }

        public static void GenerateStripEdgeLines(LaneStrip laneStrip, Action<RoadSplineComponent, RoadSplineRange> target, float voffset = 0) {
            //Get side-line flags
            var mergeLeft = (laneStrip.Spec.Flags & LaneFlags.MergeLeft) != 0;
            var mergeRight = (laneStrip.Spec.Flags & LaneFlags.MergeRight) != 0;
            var isMerge = (laneStrip.Spec.Flags & LaneFlags.IsMerge) != 0;

            if (mergeLeft && mergeRight) return;
            var swapMerges = isMerge ? laneStrip.EndLane.End == Node.NodeEnd.Backward : laneStrip.StartLane.End == Node.NodeEnd.Backward;
            if (swapMerges) DataUtil.Swap(ref mergeLeft, ref mergeRight);

            //Get tags
            var roadTag = laneStrip.Road.Bounds;

            //Generate side-lines
            var lineWidth = laneStrip.Spec.LineWidth;

            RoadSplineComponent DrawSide(LaneRange laneRange, LaneFlags flag, float bias) {
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

            var startRange = laneStrip.StartLane.Bounds;
            var endRange = laneStrip.EndLane.Bounds;
            
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

            var leftRange = LaneStripToRoadStripRange(laneStrip, new(startLeft, startLeftCenter), new(endLeft, endLeftCenter));
            var rightRange = LaneStripToRoadStripRange(laneStrip, new(startRightCenter, startRight), new(endRightCenter, endRight));
            target(DrawSide(leftRange, LaneFlags.NoLeft, 0), ToRoadSplineRange(leftRange, voffset));
            target(DrawSide(rightRange, LaneFlags.NoRight, 1), ToRoadSplineRange(rightRange, voffset));
        }

        public static RoadSplineRange ToRoadSplineRange(LaneRange laneRange, float voffset = 0) => new(
                VOffset(laneRange.startRange.Min, voffset),
                VOffset(laneRange.endRange.Min, voffset),
                VOffset(laneRange.startRange.Max, voffset),
                VOffset(laneRange.endRange.Max, voffset)
            );
        public static Vector3 VOffset(float x, float y) => new(x, y, 0);

        public static LaneRange LaneStripToRoadStripRange(LaneStrip strip, Range<float> startRange, Range<float> endRange) {
            if (strip.IsReverse()) DataUtil.Swap(ref startRange, ref endRange);
            return new(strip.Road, startRange, endRange);
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