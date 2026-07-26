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

        public static (Vector3[] Left, Vector3[] Right) GenerateSplines(LaneRange laneRange, float voffset = 0) {
            return (
                laneRange.road.GenerateSpline(laneRange.startRange.Min, laneRange.endRange.Max, voffset),
                laneRange.road.GenerateSpline(laneRange.startRange.Max, laneRange.endRange.Min, voffset)
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