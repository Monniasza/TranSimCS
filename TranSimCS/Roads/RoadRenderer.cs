using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Menus.InGame;
using TranSimCS.Render;
using static TranSimCS.Geometry;

namespace TranSimCS.Roads {
    public struct LaneQuadPair(Quad front, Quad back) {
        public Quad Front = front, Back = back;
    }

    public static class RoadRenderer {
        //Colors used by default
        public static Color SemiClearWhite => new Color(255, 255, 255, 128);
        public static Color SemiClearGray => new Color(128, 128, 128, 128);

        public static void CreateAddLanes(RoadNode nodeEnd, IRenderBin mesh, float size = 1, Color? color = null, float voffset = 0.001f) {
            if (nodeEnd.Lanes.Count < 1) return;
            var leftLimit = nodeEnd.Lanes[0].LeftPosition;
            var rightLimit = nodeEnd.Lanes[nodeEnd.Lanes.Count - 1].RightPosition;
            CreateAddLane(new AddLaneSelection(-1, leftLimit, nodeEnd.FrontEnd), mesh, size, color, voffset);
            CreateAddLane(new AddLaneSelection(1, rightLimit, nodeEnd.FrontEnd), mesh, size, color, voffset);
            CreateAddLane(new AddLaneSelection(-1, leftLimit, nodeEnd.RearEnd), mesh, size, color, voffset);
            CreateAddLane(new AddLaneSelection(1, rightLimit, nodeEnd.RearEnd), mesh, size, color, voffset);
        }
        public static Quad CreateAddLane(AddLaneSelection als, IRenderBin mesh, float size = 1, Color? color = null, float voffset = 0.001f) {
            var zrange = Geometry.RoadEndToRange(als.nodeEnd.End) * size;
            var xrange = als.CalculateOffsets(size);
            Quad quad = GenerateLaneQuad(als.nodeEnd.Node, xrange.X, xrange.Y, color ?? SemiClearGray, voffset, zrange.X, zrange.Y);
            mesh.DrawQuad(quad);
            mesh.AddTagsToLastTriangles(2, als);
            return quad;
        }
        public static Quad GenerateRoadNodeSelQuad(RoadNode node, Color color, float voffset = 0) {
            return GenerateLaneQuad(node, node.Lanes[0].LeftPosition, node.Lanes[node.Lanes.Count - 1].RightPosition, color, voffset);
        }
        public static void GenerateRoadNodeMesh(RoadNode node, IRenderBin renderBin, float voffset = 0) {
            foreach(var lane in node.Lanes) GenerateLaneMesh(lane, renderBin, voffset);
        }
        public static void GenerateLaneMesh(Lane lane, IRenderBin renderBin, float voffset = 0) {
            var quads = GenerateLaneQuad(lane, voffset);
            renderBin.DrawQuad(quads.Front);
            renderBin.AddTagsToLastTriangles(2, lane.Front);
            renderBin.DrawQuad(quads.Back);
            renderBin.AddTagsToLastTriangles(2, lane.Rear);
        }
        public static LaneQuadPair GenerateLaneQuad(Lane lane, float voffset = 0, Color? color = null) {
            var altColor = lane.Spec.Color;
            altColor.A /= 2;
            return GenerateLaneQuads(lane.RoadNode, lane.LeftPosition, lane.RightPosition, color ?? altColor, voffset);
        }
        public static Quad GenerateLaneQuad(LaneEnd lane, float voffset = 0, Color? color = null) {
            var altColor = lane.lane.Spec.Color;
            altColor.A /= 2;
            var range = Geometry.RoadEndToRange(lane.end);
            return GenerateLaneQuad(lane.lane.RoadNode, lane.lane.LeftPosition, lane.lane.RightPosition, color ?? altColor, voffset, range.X, range.Y);
        }
        public static LaneQuadPair GenerateLaneQuads(RoadNode node, float lb, float rb, Color color, float voffset = 0) {
            Vector3 offset = new Vector3(0, voffset, 0);
            Transform3 transform = node.PositionProp.Value.CalcReferenceFrame();
            var vl = transform.O + lb * transform.X;
            var vr = transform.O + rb * transform.X;
            var vd = vl - transform.Z;
            var vc = vr - transform.Z;
            var va = vl + transform.Z;
            var vb = vr + transform.Z;
            Quad front = new Quad(va, vb, vr, vl, color) + offset;
            Quad back = new Quad(vl, vr, vc, vd, color) + offset;
            return new LaneQuadPair(front, back);
        }

        public static Quad GenerateLaneQuad(RoadNode node, float lb, float rb, Color color, float voffset = 0, float minZ = -1, float maxZ = 1) {
            Vector3 offset = new Vector3(0, voffset, 0);
            Transform3 transform = node.PositionProp.Value.CalcReferenceFrame();
            var vl = transform.O + lb * transform.X;
            var vr = transform.O + rb * transform.X;
            var vd = vl + transform.Z * minZ;
            var vc = vr + transform.Z * minZ;
            var va = vl + transform.Z * maxZ;
            var vb = vr + transform.Z * maxZ;
            return new Quad(va, vb, vc, vd, color) + offset; 
        }


        /// <summary>
        /// Generates the mesh for a road segment. Does not contain any lane meshes
        /// </summary>
        /// <param name="connection">road segment</param>
        /// <param name="renderHelper">render helper</param>
        public static void GenerateRoadSegmentFullMesh(RoadStrip connection, IRenderBin renderHelper, float voffset = 0) {
            
        }
        public static void RenderRoadSegment(RoadStrip connection, IRenderBin renderHelper, float voffset = 0) {
            foreach(var lane in connection.Lanes) { // Iterate through each lane in the road segment
                renderHelper.DrawModel(lane.GetMesh()); // Draw the mesh of the lane with the specified vertical offset
            }
        }
        public static void GenerateLaneStripMesh(LaneStrip laneStrip, IRenderBin renderer, float voffset = 0) {
            var tag = laneStrip.Tag;
            GenerateLaneRangeMesh(tag, renderer, laneStrip.Spec.Color, voffset, laneStrip); // Generate the lane tag mesh
        }

        public static void GenerateLaneRangeMesh(LaneRange range, IRenderBin renderer, Color color, float voffset = 0, object tag = null) {
            var offset = new Vector3(0, voffset, 0); // Offset for the lane position

            var strips = GenerateSplines(range, voffset); // Generate the splines for the left and right lanes

            //Generate border curves
            Vector3[] leftBorder = GenerateSplinePoints(strips.Item1, 10);
            Vector3[] rightBorder = GenerateSplinePoints(strips.Item2, 10);

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

        public static (Bezier3, Bezier3) GenerateSplines(LaneRange laneTag, float voffset = 0) {
            return GenerateSplines(laneTag.startLaneIndexL, laneTag.startLaneIndexR, laneTag.startSide, laneTag.endLaneIndexL, laneTag.endLaneIndexR, laneTag.endSide, voffset);
        }

        public static (Bezier3, Bezier3) GenerateSplines(LaneEnd laneIndexStart, LaneEnd laneIndexEnd, float voffset = 0) {
            return GenerateSplines(laneIndexStart.lane, laneIndexStart.lane, laneIndexStart.end, laneIndexEnd.lane, laneIndexEnd.lane, laneIndexEnd.end, voffset);
        }

        public static (Bezier3, Bezier3) GenerateSplines(Lane laneIndexStartL, Lane laneIndexStartR, NodeEnd dirStart, Lane laneIndexEndL, Lane laneIndexEndR, NodeEnd dirEnd, float voffset = 0) {
            var offset = new Vector3(0, voffset, 0); // Offset for the lane position
            dirEnd = dirEnd.Negate();

            var n1l = laneIndexStartL.RoadNode; // Starting road node for left lane
            var n1r = laneIndexStartR.RoadNode; // Starting road node for right lane
            var n2l = laneIndexEndL.RoadNode; // Ending road node for left lane
            var n2r = laneIndexEndR.RoadNode; // Ending road node for right lane

            var pos1L = calcLineEnd(n1l, laneIndexStartL.LeftPosition, dirStart);
            var pos1R = calcLineEnd(n1r, laneIndexStartR.RightPosition, dirStart);
            var pos2L = calcLineEnd(n2l, laneIndexEndL.LeftPosition, dirEnd);
            var pos2R = calcLineEnd(n2r, laneIndexEndR.RightPosition, dirEnd);
            LineEnd tmp;

            //Ensure the node ordering

            if(dirStart == NodeEnd.Backward) {
                tmp = pos1L;
                pos1L = pos1R;
                pos1R = tmp;
                
            }
            if (dirEnd == NodeEnd.Backward) {
                tmp = pos2L;
                pos2L = pos2R;
                pos2R = tmp;
            }
            return (
                GenerateJoinSpline(pos1L.Position + offset, pos2L.Position + offset, pos1L.Tangential, pos2L.Tangential),
                GenerateJoinSpline(pos1R.Position + offset, pos2R.Position + offset, pos1R.Tangential, pos2R.Tangential)
            );
        }

        public static void DrawBezierStrip(Bezier3 lbound, Bezier3 rbound, IRenderBin renderer, Color color) {
            Vector3[] leftBorder = GenerateSplinePoints(lbound, 10);
            Vector3[] rightBorder = GenerateSplinePoints(rbound, 10);

            var leftBorder2 = GeneratePositionsFromVectors(0, color, leftBorder);
            var rightBorder2 = GeneratePositionsFromVectors(1, color, rightBorder);
            var strip = WeaveStrip(leftBorder2, rightBorder2);
            var triangleCount = strip.Length - 2; // Each triangle is formed by 3 vertices, so the number of triangles is the number of vertices minus 2
            renderer.DrawStrip(strip);
        }

        internal static void GenerateSectionMesh(RoadSection roadSection, IRenderBin mesh, int accuracy = 17) {
            //Rotate the list so the 1st main end lies on the index 0
            var endsPair = roadSection.MainSlopeNodes.Value;
            var start = endsPair.Start;
            var end = endsPair.End;

            //Generate bounding edges
            var startLeft = Geometry.calcBoundingLineEndFaced(start, -1);
            var startRight = Geometry.calcBoundingLineEndFaced(start, 1);
            var endRight = Geometry.calcBoundingLineEndFaced(end, 1);
            var endLeft = Geometry.calcBoundingLineEndFaced(end, -1);

            var leftEdge = Geometry.GenerateSplinePoints(startLeft.Position, endRight.Position, startLeft.Tangential, endRight.Tangential,  accuracy);
            var rightEdge = Geometry.GenerateSplinePoints(startRight.Position, endLeft.Position, startRight.Tangential, endLeft.Tangential, accuracy);
            var wovenStrip = WeaveStrip(leftEdge, rightEdge);
            var stripVerts = wovenStrip.Select(Geometry.CreateVertex).ToArray();

            mesh.DrawStrip(stripVerts);

            //Find the nodes in the circular list
            var circularList = DLNode<RoadNodeEnd>.CreateCircular(roadSection.Nodes);
            var startNode = circularList;
            while (startNode.val != start) startNode = startNode.Next;
            var endNode = circularList;
            while (endNode.val != end) endNode = endNode.Next;

            //Categorize the nodes into categories: left or right of the main. Since they're already sorted, there's no need to sort.
            var rightNodes = new List<RoadNodeEnd>();
            var rightNode = startNode;
            while(rightNode != endNode) {
                rightNodes.Add(rightNode.val);
                rightNode = rightNode.Next;
            }
            rightNodes.Add(rightNode.val);

            var leftNodes = new List<RoadNodeEnd>();
            var leftNode = endNode;
            while (leftNode != startNode) {
                leftNodes.Add(leftNode.val);
                leftNode = leftNode.Next;
            }
            leftNodes.Add(leftNode.val);
            Debug.Print($"{leftNodes.Count} lnodes {rightNodes.Count} rnodes");

            //Generate the main strip
            var revLeftEdge = new List<Vector3>(leftEdge);
            revLeftEdge.Reverse();

            //Generate other vertices on the right
            GenerateSubrangeVerts(mesh, revLeftEdge.ToArray(), Color.Blue, rightNodes.ToArray());

            //Generate other vertices on the left
            GenerateSubrangeVerts(mesh, rightEdge, Color.Red, leftNodes.ToArray());
        }

        public static void GenerateSubrangeVerts(IRenderBin mesh, Vector3[] stripBound, Color debugColor, params RoadNodeEnd[] nodes) {
            var generatedPoints = new List<Vector3>();
            for (int i = 1; i < nodes.Length; i++) {
                var prev = nodes[i - 1];
                var next = nodes[i];
                var prevLeftRay = Geometry.calcBoundingLineEndFaced(prev, -1);
                var nextRightRay = Geometry.calcBoundingLineEndFaced(next, 1);
                var generatedEdge = Geometry.GenerateSplinePoints(prevLeftRay.Position, nextRightRay.Position, prevLeftRay.Tangential, nextRightRay.Tangential);
                generatedPoints.AddRange(generatedEdge);
            }

            //Add points from the strip bound EXCEPT the beginning and end
            generatedPoints.AddRange(stripBound.Skip(1).Take(stripBound.Length-2));
            generatedPoints.Reverse();
            var mappedPoints = generatedPoints.Select(Geometry.CreateVertex).ToArray();
            //EarClipping.DrawEarClipping(mesh, mappedPoints);

            //DEBUG: Draw a fence that is visible only from the inside
            var pointCount = generatedPoints.Count();
            var height = Vector3.UnitY * 5;
            var bottomEdge = new VertexPositionColorTexture[pointCount+1];
            var topEdge = new VertexPositionColorTexture[pointCount+1];
            for(int i = 0; i < pointCount; i++) {
                var pos = generatedPoints[i];
                var topCoord = new VertexPositionColorTexture(pos, debugColor, new(0, i));
                var bottomCoord = new VertexPositionColorTexture(pos+height, debugColor, new(1, i));
                bottomEdge[i] = bottomCoord;
                topEdge[i] = topCoord;
            }
            topEdge[pointCount] = topEdge[0];
            bottomEdge[pointCount] = bottomEdge[0];
            var weave = Geometry.WeaveStrip(topEdge, bottomEdge);
            mesh.DrawStrip(weave);
        }
    }
}