using System;
using Microsoft.Xna.Framework;
using TranSimCS.Roads;
using static TranSimCS.Geometry;

namespace TranSimCS {
    public static class RoadRenderer {
        /// <summary>
        /// Generates the mesh for a road segment.
        /// </summary>
        /// <param name="connection">road segment</param>
        /// <param name="renderHelper">render helper</param>
        public static void GenerateRoadSegmentBoundingMesh(RoadStrip connection, IRenderBin renderHelper, float voffset = 0) {
            
        }
        public static void RenderRoadSegment(RoadStrip connection, IRenderBin renderHelper, float voffset = 0) {
            foreach(var lane in connection.Lanes) { // Iterate through each lane in the road segment
                renderHelper.DrawModel(lane.GetMesh()); // Draw the mesh of the lane with the specified vertical offset
            }
        }
        public static void GenerateLaneStripMesh(LaneStrip laneStrip, IRenderBin renderer, float voffset = 0) {
            var startLane = laneStrip.StartLane; // Starting lane of the lane strip
            var endLane = laneStrip.EndLane; // Ending lane of the lane strip
            var tag = new LaneRange(laneStrip.road, startLane, startLane, endLane, endLane); // Create a tag for the lane
            GenerateLaneRangeMesh(tag, renderer, laneStrip.spec.Color, voffset, laneStrip); // Generate the lane tag mesh
        }

        public static void GenerateLaneRangeMesh(LaneRange range, IRenderBin renderer, Color color, float voffset = 0, object tag = null) {
            var offset = new Vector3(0, voffset, 0); // Offset for the lane position

            var strips = GenerateSplines(range, voffset); // Generate the splines for the left and right lanes

            //Generate border curves
            Vector3[] leftBorder = Geometry.GenerateSplinePoints(strips.Item1, 10);
            Vector3[] rightBorder = Geometry.GenerateSplinePoints(strips.Item2, 10);

            var leftBorder2 = Geometry.GeneratePositionsFromVectors(0, color, leftBorder);
            var rightBorder2 = Geometry.GeneratePositionsFromVectors(1, color, rightBorder);
            var strip = Geometry.WeaveStrip(leftBorder2, rightBorder2);
            var triangleCount = strip.Length - 2; // Each triangle is formed by 3 vertices, so the number of triangles is the number of vertices minus 2

            //Draw strip representing the lane
            renderer.DrawStrip(strip);

            //Apply the tag to the last triangles in the strip
            object tagToUse = tag ?? range; // Use the provided tag or the lane range as the default tag
            renderer.AddTagsToLastTriangles(triangleCount, tagToUse); // Add tags to the last triangles in the strip
        }

        public static (Bezier3, Bezier3) GenerateSplines(LaneRange laneTag, float voffset = 0) {
            return GenerateSplines(laneTag.startLaneIndexL, laneTag.startLaneIndexR, laneTag.endLaneIndexL, laneTag.endLaneIndexR, voffset);
        }

        public static (Bezier3, Bezier3) GenerateSplines(Lane laneIndexStart, Lane laneIndexEnd, float voffset = 0) {
            return GenerateSplines(laneIndexStart, laneIndexStart, laneIndexEnd, laneIndexEnd, voffset);
        }

        public static (Bezier3, Bezier3) GenerateSplines(Lane laneIndexStartL, Lane laneIndexStartR, Lane laneIndexEndL, Lane laneIndexEndR, float voffset = 0) {
            var offset = new Vector3(0, voffset, 0); // Offset for the lane position   

            var n1l = laneIndexStartL.RoadNode; // Starting road node for left lane
            var n1r = laneIndexStartR.RoadNode; // Starting road node for right lane
            var n2l = laneIndexEndL.RoadNode; // Ending road node for left lane
            var n2r = laneIndexEndR.RoadNode; // Ending road node for right lane

            var pos1L = Geometry.calcLineEnd(n1l, laneIndexStartL.LeftPosition);
            var pos1R = Geometry.calcLineEnd(n1r, laneIndexStartR.RightPosition);
            var pos2L = Geometry.calcLineEnd(n2l, laneIndexEndL.LeftPosition);
            var pos2R = Geometry.calcLineEnd(n2r, laneIndexEndR.RightPosition);
            return (
                Geometry.GenerateJoinSpline(pos1L.Position + offset, pos2L.Position + offset, pos1L.Tangential, pos2L.Tangential),
                Geometry.GenerateJoinSpline(pos1R.Position + offset, pos2R.Position + offset, pos1R.Tangential, pos2R.Tangential));
        }

        public static void DrawBezierStrip(Bezier3 lbound, Bezier3 rbound, IRenderBin renderer, Color color) {
            Vector3[] leftBorder = Geometry.GenerateSplinePoints(lbound, 10);
            Vector3[] rightBorder = Geometry.GenerateSplinePoints(rbound, 10);

            var leftBorder2 = Geometry.GeneratePositionsFromVectors(0, color, leftBorder);
            var rightBorder2 = Geometry.GeneratePositionsFromVectors(1, color, rightBorder);
            var strip = Geometry.WeaveStrip(leftBorder2, rightBorder2);
            var triangleCount = strip.Length - 2; // Each triangle is formed by 3 vertices, so the number of triangles is the number of vertices minus 2
            renderer.DrawStrip(strip);
        }
    }
}