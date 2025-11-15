using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Render;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;
using static TranSimCS.Geometry.GeometryUtils;
using static TranSimCS.Geometry.LineEnd;

namespace TranSimCS.Roads {
    public struct LaneQuadPair<T>(Quad<T> front, Quad<T> back) {
        public Quad<T> Front = front, Back = back;
    }

    public static class RoadRenderer {
        //Colors used by default
        public static Color SemiClearWhite => new Color(255, 255, 255, 128);
        public static Color SemiClearGray => new Color(128, 128, 128, 128);

        public static void CreateAddLanes(RoadNode nodeEnd, MeshBuilder<SimpleMaterial, VertexPositionColorTexture> mesh, float size = 1, Color? color = null, float voffset = 0.2f) {
            if (nodeEnd.Lanes.Count < 1) return;
            var leftLimit = nodeEnd.Lanes[0].LeftPosition;
            var rightLimit = nodeEnd.Lanes[nodeEnd.Lanes.Count - 1].RightPosition;
            CreateAddLane(new AddLaneSelection(-1, leftLimit, nodeEnd.FrontEnd), mesh, size, color, voffset);
            CreateAddLane(new AddLaneSelection(1, rightLimit, nodeEnd.FrontEnd), mesh, size, color, voffset);
            CreateAddLane(new AddLaneSelection(-1, leftLimit, nodeEnd.RearEnd), mesh, size, color, voffset);
            CreateAddLane(new AddLaneSelection(1, rightLimit, nodeEnd.RearEnd), mesh, size, color, voffset);
        }
        public static Quad<VertexPositionColorTexture> CreateAddLane(AddLaneSelection als, MeshBuilder<SimpleMaterial, VertexPositionColorTexture> mesh, float size = 1, Color? color = null, float voffset = 0.2f) {
            var zrange = RoadEndToRange(als.nodeEnd.End) * size;
            var xrange = als.CalculateOffsets(size);
            var quad = GenerateLaneQuad(als.nodeEnd.Node, xrange.X, xrange.Y, color ?? SemiClearGray, voffset, zrange.X, zrange.Y);
            mesh.DrawQuad(quad);
            return quad;
        }
        public static Quad<VertexPositionColorTexture> GenerateRoadNodeSelQuad(RoadNode node, Color color, float voffset = 0.2f) {
            return GenerateLaneQuad(node, node.Lanes[0].LeftPosition, node.Lanes[node.Lanes.Count - 1].RightPosition, color, voffset);
        }
        public static void GenerateRoadNodeMesh(RoadNode node, MeshComplex renderBin, float voffset = 0) {
            foreach(var lane in node.Lanes) GenerateLaneMesh(lane, renderBin, voffset);
        }
        public static void GenerateLaneMesh(Lane lane, MeshComplex mesh, float voffset = 0) {
            MeshBuilder<SimpleMaterial, VertexPositionColorTexture> renderBin = new MeshBuilder<SimpleMaterial, VertexPositionColorTexture>();
            renderBin.Name = MeshElement.GoodName(lane.RoadNode, "lane", lane.Index.ToString());

            var quads = GenerateLaneQuad(lane, voffset);
            renderBin.DrawQuad(quads.Back, lane.Rear);
            renderBin.DrawQuad(quads.Front, lane.Front);
        }
        public static LaneQuadPair<VertexPositionColorTexture> GenerateLaneQuad(Lane lane, float voffset = 0.2f, Color? color = null) {
            var altColor = lane.Spec.Color;
            altColor.A /= 2;
            var rear = GenerateLaneQuad(lane.Rear, voffset, color);
            var front = GenerateLaneQuad(lane.Front, voffset, color);
            return new(front, rear);
        }
        public static Quad<VertexPositionColorTexture> GenerateLaneQuad(LaneEnd lane, float voffset = 0.2f, Color? color = null) {
            var altColor = lane.lane.Spec.Color;
            altColor.A /= 2;
            var range = GeometryUtils.RoadEndToRange(lane.end);
            return GenerateLaneQuad(lane.lane.RoadNode, lane.lane.LeftPosition, lane.lane.RightPosition, color ?? altColor, lane, voffset, range.X, range.Y);
        }

        public static Quad<VertexPositionColorTexture> GenerateLaneQuad(RoadNode node, float lb, float rb, Color color, object? tag = null, float voffset = 0.2f, float minZ = -1, float maxZ = 1) {
            Vector3 offset = new Vector3(0, voffset, 0);
            Transform3 transform = node.PositionProp.Value.CalcReferenceFrame();
            var vl = transform.O + lb * transform.X + offset;
            var vr = transform.O + rb * transform.X + offset;
            var vd = vl + transform.Z * minZ;
            var vc = vr + transform.Z * minZ;
            var va = vl + transform.Z * maxZ;
            var vb = vr + transform.Z * maxZ;
            return Quads.Create(va, vb, vc, vd, color);
        }

        public static void GenerateLaneRangeMesh(LaneRange range, MeshBuilder<SimpleMaterial, VertexPositionColorTexture> renderer, Color color, float voffset = 0.3f, object? tag = null) {
            var strips = GenerateSplines(range, voffset); // Generate the splines for the left and right lanes

            //Generate border curves
            Vector3[] leftBorder = GenerateSplinePoints(strips.Item1);
            Vector3[] rightBorder = GenerateSplinePoints(strips.Item2);

            var leftBorder2 = GeneratePositionsFromVectors(0, color, leftBorder);
            var rightBorder2 = GeneratePositionsFromVectors(1, color, rightBorder);
            var strip = WeaveStrip(leftBorder2, rightBorder2);
            var triangleCount = strip.Length - 2; // Each triangle is formed by 3 vertices, so the number of triangles is the number of vertices minus 2

            //Draw strip representing the lane
            renderer.DrawStrip(strip, tag);
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
            var pos2L = calcLineEnd(n2l, laneIndexEndL.LeftPosition, dirEnd.Negate());
            var pos2R = calcLineEnd(n2r, laneIndexEndR.RightPosition, dirEnd.Negate());
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

        public static void DrawBezierStrip(Bezier3 lbound, Bezier3 rbound, MeshBuilder<SimpleMaterial, VertexPositionColorTexture> renderer, Color color) {
            Vector3[] leftBorder = GenerateSplinePoints(lbound, 10);
            Vector3[] rightBorder = GenerateSplinePoints(rbound, 10);

            var leftBorder2 = GeneratePositionsFromVectors(0, color, leftBorder);
            var rightBorder2 = GeneratePositionsFromVectors(1, color, rightBorder);
            var strip = WeaveStrip(leftBorder2, rightBorder2);
            var triangleCount = strip.Length - 2; // Each triangle is formed by 3 vertices, so the number of triangles is the number of vertices minus 2
            renderer.DrawStrip(strip);
        }

        internal static void GenerateSectionMesh(RoadSection roadSection, MeshComplex multimesh, int accuracy = 17, float voffset = 0.3f) {
            var meshBuilder = new MeshBuilder<SimpleMaterial, VertexPositionColorTexture>();
            var mat = new SimpleMaterial(Assets.Asphalt);
            meshBuilder.Material = mat;

            //Rotate the list so the 1st main end lies on the index 0
            var endsPair = roadSection.MainSlopeNodes.Value;
            var start = endsPair.Start;
            var end = endsPair.End;

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

            //Generate the main strip with vertical offset to prevent Z-fighting
            GenerateIntersectionStrip(meshBuilder, start, end, accuracy, voffset);

            //Generate other vertices on the right
            GenerateSubrangeVerts(meshBuilder, rightNodes.ToArray(), 1, accuracy, voffset);

            //Generate other vertices on the left
            GenerateSubrangeVerts(meshBuilder, leftNodes.ToArray(), -1, accuracy, voffset);

            meshBuilder.Name = MeshElement.GoodName(roadSection, "asphalt");
        }

        public static void GenerateSubrangeVerts(MeshBuilder<SimpleMaterial, VertexPositionColorTexture> mesh, RoadNodeEnd[] nodes, int discriminant, int accuracy = 17, float voffset = 0f) {
            var lbound = 1;
            var ubound = nodes.Length - 2;

            var prevSpline = GenerateRoadEdge(nodes[0], nodes[nodes.Length-1], -1).Inverse();

            while (lbound <= ubound) {
                var preNode = nodes[lbound - 1];
                var startNode = nodes[lbound];
                var endNode = nodes[ubound];
                var nextNode = nodes[ubound + 1];
                var color = Color.White;
                ISpline<Vector3> topSpline;
                ISpline<Vector3> bottomSpline;
                ISpline<Vector3> leftSpline;
                ISpline<Vector3> rightSpline;

                var preToStartSpline = GenerateRoadEdge(preNode, startNode, -1);
                var nextToEndSpline = GenerateRoadEdge(nextNode, endNode, 1);

                if (lbound == ubound) {
                    var bounds = startNode.Bounds();
                    var lpos = calcLineEnd(startNode, bounds.X).Position;
                    var rpos = calcLineEnd(startNode, bounds.Y).Position;

                    //One node remaining
                    topSpline = prevSpline;
                    bottomSpline = new LineSegment(rpos, lpos);
                    leftSpline = preToStartSpline;
                    rightSpline = nextToEndSpline;
                } else {
                    //More nodes remaining
                    var ledge = GenerateRoadEdge(startNode, endNode, -discriminant);
                    var redge = GenerateRoadEdge(startNode, endNode, discriminant);
                    var innerSpline = (discriminant < 0) ? redge : ledge;
                    var outerSpline = (discriminant < 0) ? ledge : redge;

                    //Calculations for the fill patch
                    topSpline = prevSpline;
                    bottomSpline = outerSpline;
                    leftSpline = preToStartSpline;
                    rightSpline = nextToEndSpline;

                    //Draw the road strip with vertical offset
                    GenerateIntersectionStrip(mesh, startNode, endNode, accuracy, voffset);

                    prevSpline = innerSpline;
                }

                //Render the last-node or the inter-strip patch with vertical offset
                var offset = new Vector3(0, voffset, 0);
                RenderPatch.RenderCoonsPatch(mesh, bottomSpline, topSpline, leftSpline.Inverse(), rightSpline.Inverse(), (p, uv) => CreateVertex(p + offset, color), accuracy, accuracy);

                lbound++;
                ubound--;
            }
        }

        /// <summary>
        /// Generates a road edge from <paramref name="start"/> to <paramref name="end"/>, at left if <paramref name="discriminant"/>&lt;0, at the right otherwise
        /// </summary>
        /// <param name="start">start node</param>
        /// <param name="end">end node</param>
        /// <param name="discriminant">determines the sidea</param>
        /// <returns></returns>
        public static Bezier3 GenerateRoadEdge(RoadNodeEnd start, RoadNodeEnd end, int discriminant) {
            LineEnd startPos = calcBoundingLineEndFaced(start, discriminant);
            LineEnd endPos = calcBoundingLineEndFaced(end, -discriminant);            
            return GenerateJoinSpline(startPos.Ray, endPos.Ray);
        }

        public static void GenerateIntersectionStrip(MeshBuilder<SimpleMaterial, VertexPositionColorTexture> mesh, RoadNodeEnd start, RoadNodeEnd end, int accuracy = 17, float voffset = 0f) {
            //Generate bounding edges
            var startLeft = calcBoundingLineEndFaced(start, -1);
            var startRight = calcBoundingLineEndFaced(start, 1);
            var endRight = calcBoundingLineEndFaced(end, 1);
            var endLeft = calcBoundingLineEndFaced(end, -1);

            var leftEdge = GenerateSplinePoints(startLeft.Position, endRight.Position, startLeft.Tangential, endRight.Tangential, accuracy);
            var rightEdge = GenerateSplinePoints(startRight.Position, endLeft.Position, startRight.Tangential, endLeft.Tangential, accuracy);
            var wovenStrip = WeaveStrip(leftEdge, rightEdge);

            // Apply vertical offset to prevent Z-fighting with ground
            var offset = new Vector3(0, voffset, 0);
            var stripVerts = wovenStrip.Select(p => CreateVertex(p + offset)).ToArray();

            mesh.DrawStrip(stripVerts);
        }
    }
}