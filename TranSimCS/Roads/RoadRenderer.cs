using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using TranSimCS.Debugging;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.Render;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Section;
using TranSimCS.Roads.Strip;
using TranSimCS.Setting;
using TranSimCS.Spline;
using static TranSimCS.Geometry.GeometryUtils;
using static TranSimCS.Geometry.LineEnd;

namespace TranSimCS.Roads {
    public struct LaneQuadPair(QuadOld front, QuadOld back) {
        public QuadOld Front = front, Back = back;
    }

    public static class RoadRenderer {
        //Colors used by default
        public static Color SemiClearWhite => new Color(255, 255, 255, 128);
        public static Color SemiClearGray => new Color(128, 128, 128, 128);

        

        public static void GenerateLaneRangeMesh(LaneRange range, Mesh renderer, Color color, float voffset = 0.3f, object? tag = null) {
            var accuracy = Settings.RoadAccuracy;
            var strips = GenerateSplines(range, voffset); // Generate the splines for the left and right lanes

            //Generate border curves
            Vector3[] leftBorder = GenerateSplinePoints(strips.left, accuracy);
            Vector3[] rightBorder = GenerateSplinePoints(strips.right, accuracy);

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

        public static (float pos1L, float pos1R, float pos2L, float pos2R) GetPositionsForGenerateSplines(LaneRange laneRange) {
            var pos1L = laneRange.startRange.Min;
            var pos1R = laneRange.startRange.Max;
            var pos2L = laneRange.endRange.Max;
            var pos2R = laneRange.endRange.Min;
            //Ensure the node ordering
            if (laneRange.road.StartNode.End == NodeEnd.Backward) (pos1L, pos1R) = (pos1R, pos1L);
            if (laneRange.road.EndNode.End == NodeEnd.Backward) (pos2L, pos2R) = (pos2R, pos2L);
            return (pos1L, pos1R, pos2L, pos2R);
        }
        public static (float pos1L, float pos1R, float pos2L, float pos2R) GetPositionsForGenerateSplines2(LaneRange laneRange) {
            var pos1L = laneRange.startRange.Min;
            var pos1R = laneRange.startRange.Max;
            var pos2L = laneRange.endRange.Max;
            var pos2R = laneRange.endRange.Min;
            //Ensure the node ordering
            if (laneRange.road.StartNode.End == NodeEnd.Backward) (pos1L, pos1R) = (pos1R, pos1L);
            if (laneRange.road.EndNode.End == NodeEnd.Forward) (pos2L, pos2R) = (pos2R, pos2L);
            return (pos1L, pos1R, pos2L, pos2R);
        }

        public static SplineStrip GenerateSplines(LaneRange laneRange, float voffset = 0) {
            var (pos1L, pos1R, pos2L, pos2R) = GetPositionsForGenerateSplines(laneRange);
            return new(
                laneRange.road.GenerateSpline(pos1L, pos2L, voffset),
                laneRange.road.GenerateSpline(pos1R, pos2R, voffset)
            );
        }

        public static void DrawBezierStrip(Bezier3 lbound, Bezier3 rbound, Mesh renderer, Color color, int accuracy = -1) {
            if (accuracy < 2) accuracy = Settings.RoadAccuracy;
            Vector3[] leftBorder = GenerateSplinePoints(lbound, accuracy);
            Vector3[] rightBorder = GenerateSplinePoints(rbound, accuracy);

            var leftBorder2 = GeneratePositionsFromVectors(0, color, leftBorder);
            var rightBorder2 = GeneratePositionsFromVectors(1, color, rightBorder);
            var strip = WeaveStrip(leftBorder2, rightBorder2);
            renderer.DrawStrip(strip);
        }
    }
}