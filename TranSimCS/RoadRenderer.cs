using System;
using Microsoft.Xna.Framework;
using static TranSimCS.Geometry;

namespace TranSimCS {
    public static class RoadRenderer {
        /// <summary>
        /// Generates the mesh for a road segment.
        /// </summary>
        /// <param name="connection">road segment</param>
        /// <param name="renderHelper">render helper</param>
        public static void RenderRoadSegment(LaneConnection connection, RenderHelper renderHelper) {
            RenderRoadSegment(connection, renderHelper.GetOrCreateRenderBin(Game1.roadTexture));
        }
        public static void DrawLaneTag(LaneTag tag, IRenderBin bin, Color? replaceColor = null, float voffset = 0) {
            DrawLaneRange(tag.startLaneIndexL, tag.startLaneIndexR, tag.endLaneIndexL, tag.endLaneIndexR, tag.road, bin, replaceColor, voffset);
        }
        /// <summary>
        /// Generates the mesh for a road segment.
        /// </summary>
        /// <param name="connection">road segment</param>
        /// <param name="renderHelper">render bin with the lane texture</param>
        public static void RenderRoadSegment(LaneConnection connection, IRenderBin renderHelper, Color? replaceColor = null, float voffset = 0) {
            //Calculate lane balances
            int startingLanes = connection.RightStartIndex - connection.LeftStartIndex; // How many lanes are open at the start node
            int endingLanes = connection.RightEndIndex - connection.LeftEndIndex; // How many lanes are open at the start node
            int leftShift = connection.EndShift - connection.StartShift; // How many lanes open to the left (negative means that the left lanes close)
            int rightShift = endingLanes - startingLanes - leftShift; // How many lanes open to the right (negative means that the right lanes close)
            int totalLanes = startingLanes + Math.Abs(leftShift) + Math.Abs(rightShift); // Total lanes after the connection
            int closingLeftLanes = Math.Max(0, -leftShift); // How many lanes close to the left
            int openingLeftLanes = Math.Max(0, leftShift); // How many lanes open to the left
            int closingRightLanes = Math.Max(0, -rightShift); // How many lanes close to the right
            int openingRightLanes = Math.Max(0, rightShift); // How many lanes open to the right

            //Calculate unchanging lanes
            int unchangingLanesStartLeft = connection.LeftStartIndex + closingLeftLanes;
            int unchangingLanesStartRight = connection.RightStartIndex - closingRightLanes;
            int unchangingLanesEndLeft = connection.LeftEndIndex + openingLeftLanes;
            int unchangingLanesEndRight = connection.RightEndIndex - openingRightLanes;
            int unchangingLanesCount = unchangingLanesStartRight - unchangingLanesStartLeft; // How many lanes remain unchanged
                                                                                             //Draw changing lanes
            RoadRenderer.DrawLaneRange(connection.LeftStartIndex, unchangingLanesStartLeft, connection.LeftEndIndex, unchangingLanesEndLeft, connection, renderHelper, replaceColor, voffset);
            RoadRenderer.DrawLaneRange(unchangingLanesStartRight, connection.RightStartIndex, unchangingLanesEndRight, connection.RightEndIndex, connection, renderHelper, replaceColor, voffset);

            //Draw the unchanged lanes
            for (int i = 0; i < unchangingLanesCount; i++) {
                int startLaneIndex = unchangingLanesStartLeft + i; // Calculate the lane index at the start node
                int endLaneIndex = unchangingLanesEndLeft + i; // Calculate the lane index at the end node
                RoadRenderer.DrawLane(startLaneIndex, endLaneIndex, connection, renderHelper, replaceColor, voffset);
            }
        }

        public static void DrawLane(int laneIndexStart, int laneIndexEnd, LaneConnection connection, IRenderBin renderer, Color? replaceColor = null, float voffset = 0) {
            DrawLaneRange(laneIndexStart, laneIndexStart + 1, laneIndexEnd, laneIndexEnd + 1, connection, renderer, replaceColor, voffset);
        }

        public static (Bezier3, Bezier3) GenerateSplines(LaneTag laneTag, float voffset = 0) {
            return GenerateSplines(laneTag.startLaneIndexL, laneTag.startLaneIndexR, laneTag.endLaneIndexL, laneTag.endLaneIndexR, laneTag.road, voffset);
        }

        public static (Bezier3, Bezier3) GenerateSplines(int laneIndexStart, int laneIndexEnd, LaneConnection connection, float voffset = 0) {
            return GenerateSplines(laneIndexStart, laneIndexStart + 1, laneIndexEnd, laneIndexEnd + 1, connection, voffset);
        }

        public static (Bezier3, Bezier3) GenerateSplines(int laneIndexStartL, int laneIndexStartR, int laneIndexEndL, int laneIndexEndR, LaneConnection connection, float voffset = 0) {
            var offset = new Vector3(0, voffset, 0); // Offset for the lane position
            var pos1L = Geometry.calcLineEnd2(connection.StartNode, laneIndexStartL);
            var pos1R = Geometry.calcLineEnd2(connection.StartNode, laneIndexStartR);
            var pos2L = Geometry.calcLineEnd2(connection.EndNode, laneIndexEndL);
            var pos2R = Geometry.calcLineEnd2(connection.EndNode, laneIndexEndR);
            return (
                Geometry.GenerateJoinSpline(pos1L.Position + offset, pos2L.Position + offset, pos1L.Tangential, pos2L.Tangential),
                Geometry.GenerateJoinSpline(pos1R.Position + offset, pos2R.Position + offset, pos1R.Tangential, pos2R.Tangential));
        }

        public static void DrawLaneRange(int laneIndexStartL, int laneIndexStartR, int laneIndexEndL, int laneIndexEndR, LaneConnection connection, IRenderBin renderer, Color? replaceColor = null, float voffset = 0) {
            var offset = new Vector3(0, voffset, 0); // Offset for the lane position
            var color = connection.LaneSpec.Color; // Get the color from the lane specification
            if (replaceColor != null)
                color = replaceColor.Value; // If a replacement color is provided, use it

            // Calculate the position of the lane based on the node's position and the lane index
            var pos1L = Geometry.calcLineEnd2(connection.StartNode, laneIndexStartL);
            var pos1R = Geometry.calcLineEnd2(connection.StartNode, laneIndexStartR);
            var pos2L = Geometry.calcLineEnd2(connection.EndNode, laneIndexEndL);
            var pos2R = Geometry.calcLineEnd2(connection.EndNode, laneIndexEndR);

            //Generate border curves
            Vector3[] leftBorder = Geometry.GenerateSplinePoints(pos1L.Position + offset, pos2L.Position + offset, pos1L.Tangential, pos2L.Tangential, 10);
            Vector3[] rightBorder = Geometry.GenerateSplinePoints(pos1R.Position + offset, pos2R.Position + offset, pos1R.Tangential, pos2R.Tangential, 10);

            var leftBorder2 = Geometry.GeneratePositionsFromVectors(0, color, leftBorder);
            var rightBorder2 = Geometry.GeneratePositionsFromVectors(1, color, rightBorder);
            var strip = Geometry.WeaveStrip(leftBorder2, rightBorder2);
            var triangleCount = strip.Length - 2; // Each triangle is formed by 3 vertices, so the number of triangles is the number of vertices minus 2
            var tag = new LaneTag(connection, laneIndexStartL, laneIndexStartR, laneIndexEndL, laneIndexEndR, connection.LaneSpec); // Create a tag for the lane

            //Draw strip representing the lane
            renderer.DrawStrip(strip);
            renderer.AddTagsToLastTriangles(triangleCount, tag); // Add tags to the last triangles in the strip
        }
    }
}
