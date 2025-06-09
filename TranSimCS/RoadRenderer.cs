using System;
using Microsoft.Xna.Framework;

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
        /// <summary>
        /// Generates the mesh for a road segment.
        /// </summary>
        /// <param name="connection">road segment</param>
        /// <param name="renderHelper">render bin with the lane texture</param>
        public static void RenderRoadSegment(LaneConnection connection, IRenderBin renderHelper) {
            // Example rendering logic for a road node
            // This is a placeholder and should be replaced with actual rendering code

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
            RoadRenderer.DrawLaneRange(connection.LeftStartIndex, unchangingLanesStartLeft, connection.LeftEndIndex, unchangingLanesEndLeft, connection, renderHelper);
            RoadRenderer.DrawLaneRange(unchangingLanesStartRight, connection.RightStartIndex, unchangingLanesEndRight, connection.RightEndIndex, connection, renderHelper);

            //Draw markers
            Vector3 pos1L = Geometry.calcLineEnd(connection.StartNode, connection.LeftStartIndex);
            Vector3 pos1R = Geometry.calcLineEnd(connection.StartNode, connection.RightStartIndex);
            Vector3 pos2L = Geometry.calcLineEnd(connection.EndNode, connection.LeftEndIndex);
            Vector3 pos2R = Geometry.calcLineEnd(connection.EndNode, connection.RightEndIndex);

            //Draw the unchanged lanes
            for (int i = 0; i < unchangingLanesCount; i++) {
                int startLaneIndex = unchangingLanesStartLeft + i; // Calculate the lane index at the start node
                int endLaneIndex = unchangingLanesEndLeft + i; // Calculate the lane index at the end node
                RoadRenderer.DrawLane(startLaneIndex, endLaneIndex, connection, renderHelper);
            }
        }

        public static void DrawLane(int laneIndexStart, int laneIndexEnd, LaneConnection connection, IRenderBin renderer) {
            DrawLaneRange(laneIndexStart, laneIndexStart + 1, laneIndexEnd, laneIndexEnd + 1, connection, renderer);
        }

        public static void DrawLaneRange(int laneIndexStartL, int laneIndexStartR, int laneIndexEndL, int laneIndexEndR, LaneConnection connection, IRenderBin renderer) {
            var color = connection.LaneSpec.Color; // Get the color from the lane specification

            // Calculate the position of the lane based on the node's position and the lane index
            var pos1L = Geometry.calcLineEnd2(connection.StartNode, laneIndexStartL);
            var pos1R = Geometry.calcLineEnd2(connection.StartNode, laneIndexStartR);
            var pos2L = Geometry.calcLineEnd2(connection.EndNode, laneIndexEndL);
            var pos2R = Geometry.calcLineEnd2(connection.EndNode, laneIndexEndR);

            //Generate border curves
            Vector3[] leftBorder = Geometry.GenerateSplinePoints(pos1L.Position, pos2L.Position, pos1L.Tangential, pos2L.Tangential, 10);
            Vector3[] rightBorder = Geometry.GenerateSplinePoints(pos1R.Position, pos2R.Position, pos1R.Tangential, pos2R.Tangential, 10);

            var leftBorder2 = Geometry.GeneratePositionsFromVectors(0, color, leftBorder);
            var rightBorder2 = Geometry.GeneratePositionsFromVectors(1, color, rightBorder);
            var strip = Geometry.WeaveStrip(leftBorder2, rightBorder2);

            //Draw strip representing the lane
            renderer.DrawStrip(strip);
        }
    }
}
