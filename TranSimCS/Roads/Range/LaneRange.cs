using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using static TranSimCS.Geometry.GeometryUtils;
using static TranSimCS.Roads.Strip.StripRenderer;

namespace TranSimCS.Roads.Range {
    public struct DualRange(Range<float> startRange, Range<float> endRange) {
        public Range<float> startRange = startRange;
        public Range<float> endRange = endRange;

        public static DualRange operator |(DualRange a, DualRange b) {
            var newEnd = a.endRange.Union(b.endRange);
            var newStart = a.startRange.Union(b.startRange);
            return new(newStart, newEnd);
        }
    }
    public struct LaneRange(RoadStrip road, Range<float> startRange, Range<float> endRange): IRoadElement {
        public RoadStrip road = road; // The road connection this tag is associated with
        public Range<float> startRange = startRange;
        public Range<float> endRange = endRange;

        //ROAD ELEMENT
        public Guid Guid => road.Guid;
        public Lane? GetLane() => null;
        public LaneStrip? GetLaneStrip() => null;
        public RoadNode? GetRoadNode() => null;
        public RoadStrip? GetRoadStrip() => road;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => 0;
        public LaneEnd? GetLaneEnd() => null;
        public RoadNodeEnd? GetNodeEnd() => null;
    }
    public static class LaneRangeMethods {
        public static DualRange ToDualRange(this LaneRange laneRange) => new(laneRange.startRange, laneRange.endRange);
        public static void GenerateLaneRangeMesh(this LaneRange range, Mesh renderer, Color color, float voffset = 0.3f, object? tag = null) {
            //Generate border curves
            var (leftBorder, rightBorder) = GenerateSplines(range, voffset); // Generate the splines for the left and right lanes

            var leftBorder2 = GeneratePositionsFromVectors(0, color, leftBorder);
            var rightBorder2 = GeneratePositionsFromVectors(1, color, rightBorder);
            var strip = WeaveStrip(leftBorder2, rightBorder2);
            var triangleCount = strip.Length - 2; // Each triangle is formed by 3 vertices, so the number of triangles is the number of vertices minus 2

            //Draw strip representing the lane
            renderer.DrawStrip(strip);

            //Apply the tag to the last triangles in the strip
            object tagToUse = tag ?? range; // Use the provided tag or the lane range as the default tag
            renderer.AddTagsToLastTriangles(triangleCount, tagToUse); // Add tags to the last triangles in the strip
        }

        public static (Vector3[] Left, Vector3[] Right) GenerateSplines(this LaneRange laneRange, float voffset = 0) {
            return (
                laneRange.road.GenerateSpline(laneRange.startRange.Min, laneRange.endRange.Max, voffset),
                laneRange.road.GenerateSpline(laneRange.startRange.Max, laneRange.endRange.Min, voffset)
            );
        }
        public static (Vector3[] Left, Vector3[] Right) GenerateSplines(this DualRange laneRange, RoadStripData basis, float voffset = 0) {
            return (
                RoadStrip.GenerateSpline(basis, laneRange.startRange.Min, laneRange.endRange.Max, voffset),
                RoadStrip.GenerateSpline(basis, laneRange.startRange.Max, laneRange.endRange.Min, voffset)
            );
        }
        public static RoadSplineRange ToRoadSplineRange(this DualRange laneRange, float voffset = 0) => new(
            VOffset(laneRange.startRange.Min, voffset),
            VOffset(laneRange.endRange.Max, voffset),
            VOffset(laneRange.startRange.Max, voffset),
            VOffset(laneRange.endRange.Min, voffset)
        );
    }
}
