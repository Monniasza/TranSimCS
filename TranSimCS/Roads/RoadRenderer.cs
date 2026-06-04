using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Debugging;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.Render;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Section;
using TranSimCS.Roads.Strip;
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

        public static void CreateAddLanes(RoadNode nodeEnd, Mesh mesh, float size = 1, Color? color = null, float voffset = 0.2f) {
            if (nodeEnd.Lanes.Count < 1) return;
            var bounds = nodeEnd.Bounds;
            var leftLimit = bounds.Min;
            var rightLimit = bounds.Max;
            CreateAddLane(new AddLaneSelection(-1, leftLimit, nodeEnd.FrontEnd), mesh, size, color, voffset);
            CreateAddLane(new AddLaneSelection(1, rightLimit, nodeEnd.FrontEnd), mesh, size, color, voffset);
            CreateAddLane(new AddLaneSelection(-1, leftLimit, nodeEnd.RearEnd), mesh, size, color, voffset);
            CreateAddLane(new AddLaneSelection(1, rightLimit, nodeEnd.RearEnd), mesh, size, color, voffset);
        }
        public static QuadOld CreateAddLane(AddLaneSelection als, Mesh mesh, float size = 1, Color? color = null, float voffset = 0.2f) {
            var zrange = RoadEndToRange(als.nodeEnd.End) * size;
            var xrange = als.CalculateOffsets(size);
            QuadOld quad = GenerateLaneQuad(als.nodeEnd.Node, xrange.Min, xrange.Max, color ?? SemiClearGray, voffset, zrange.X, zrange.Y);
            mesh.DrawQuad(quad);
            mesh.AddTagsToLastTriangles(2, als);
            return quad;
        }
        public static QuadOld GenerateRoadNodeSelQuad(RoadNode node, Color color, float voffset = 0.2f) {
            var bounds = node.Bounds;
            return GenerateLaneQuad(node, bounds.Min, bounds.Max, color, voffset);
        }
        public static void GenerateRoadNodeMesh(RoadNode node, MultiMesh renderBin, float voffset = 0) {
            foreach(var lane in node.Lanes) GenerateLaneMesh(lane, renderBin, voffset);
        }
        public static void GenerateLaneMesh(Lane lane, MultiMesh mesh, float voffset = 0) {
            Mesh renderBin = mesh.GetOrCreateRenderBinForced(Assets.Road);

            var quads = GenerateLaneQuad(lane, voffset);
            renderBin.DrawQuad(quads.Front);
            renderBin.AddTagsToLastTriangles(2, lane.Front);
            renderBin.DrawQuad(quads.Back);
            renderBin.AddTagsToLastTriangles(2, lane.Rear);
        }
        public static LaneQuadPair GenerateLaneQuad(Lane lane, float voffset = 0.2f, Color? color = null) {
            var altColor = lane.Spec.Color;
            altColor.A /= 2;
            return GenerateLaneQuads(lane.RoadNode, lane.LeftPosition, lane.RightPosition, color ?? altColor, voffset);
        }
        public static QuadOld GenerateLaneQuad(LaneEnd lane, float voffset = 0.2f, Color? color = null) {
            var altColor = lane.lane.Spec.Color;
            altColor.A /= 2;
            var range = GeometryUtils.RoadEndToRange(lane.end);
            return GenerateLaneQuad(lane.lane.RoadNode, lane.lane.LeftPosition, lane.lane.RightPosition, color ?? altColor, voffset, range.X, range.Y);
        }
        public static LaneQuadPair GenerateLaneQuads(RoadNode node, float lb, float rb, Color color, float voffset = 0) {
            Vector3 offset = new(0, voffset, 0);
            Transform3 transform = node.PositionProp.Value.CalcReferenceFrame();
            var vl = transform.O + lb * transform.X;
            var vr = transform.O + rb * transform.X;
            var vd = vl - transform.Z;
            var vc = vr - transform.Z;
            var va = vl + transform.Z;
            var vb = vr + transform.Z;
            QuadOld front = new QuadOld(va, vb, vr, vl, color) + offset;
            QuadOld back = new QuadOld(vl, vr, vc, vd, color) + offset;
            return new LaneQuadPair(front, back);
        }

        public static QuadOld GenerateLaneQuad(RoadNode node, float lb, float rb, Color color, float voffset = 0.2f, float minZ = -1, float maxZ = 1) {
            Vector3 offset = new(0, voffset, 0);
            Transform3 transform = node.PositionProp.Value.CalcReferenceFrame();
            var vl = transform.O + lb * transform.X;
            var vr = transform.O + rb * transform.X;
            var vd = vl + transform.Z * minZ;
            var vc = vr + transform.Z * minZ;
            var va = vl + transform.Z * maxZ;
            var vb = vr + transform.Z * maxZ;
            return new QuadOld(va, vb, vc, vd, color) + offset; 
        }

        public static void GenerateLaneRangeMesh(LaneRange range, Mesh renderer, Color color, float voffset = 0.3f, object? tag = null) {
            var strips = GenerateSplines(range, voffset); // Generate the splines for the left and right lanes

            //Generate border curves
            Vector3[] leftBorder = GenerateSplinePoints(strips.Item1);
            Vector3[] rightBorder = GenerateSplinePoints(strips.Item2);

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

        public static (Bezier3 Left, Bezier3 Right) GenerateSplines(LaneRange laneTag, float voffset = 0) {
            var pos1L = laneTag.startRange.Min;
            var pos1R = laneTag.startRange.Max;
            var pos2L = laneTag.endRange.Max;
            var pos2R = laneTag.endRange.Min;
            //Ensure the node ordering
            if (laneTag.road.StartNode.End == NodeEnd.Backward) (pos1L, pos1R) = (pos1R, pos1L);
            if (laneTag.road.EndNode.End == NodeEnd.Backward) (pos2L, pos2R) = (pos2R, pos2L);
            return (
                laneTag.road.GenerateSpline(pos1L, pos2L, voffset),
                laneTag.road.GenerateSpline(pos1R, pos2R, voffset)
            );
        }

        public static void DrawBezierStrip(Bezier3 lbound, Bezier3 rbound, Mesh renderer, Color color) {
            Vector3[] leftBorder = GenerateSplinePoints(lbound, 10);
            Vector3[] rightBorder = GenerateSplinePoints(rbound, 10);

            var leftBorder2 = GeneratePositionsFromVectors(0, color, leftBorder);
            var rightBorder2 = GeneratePositionsFromVectors(1, color, rightBorder);
            var strip = WeaveStrip(leftBorder2, rightBorder2);
            renderer.DrawStrip(strip);
        }
    }
}