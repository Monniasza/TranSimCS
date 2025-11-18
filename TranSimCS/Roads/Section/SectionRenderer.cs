using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TranSimCS.Debugging;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Render;
using TranSimCS.Roads.Node;
using TranSimCS.Spline;
using static TranSimCS.Geometry.GeometryUtils;
using static TranSimCS.Geometry.LineEnd;

namespace TranSimCS.Roads.Section {
    internal static class SectionRenderer {

        public static void GenerateIntersectionStrip(Mesh mesh, RoadNodeEnd start, RoadNodeEnd end, int accuracy = 17, float voffset = 0f) {
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

        public static void GenerateSubrangeVerts(Mesh mesh, RoadNodeEnd[] nodes, int discriminant, int accuracy = 17, float voffset = 0f) {
            var lbound = 1;
            var ubound = nodes.Length - 2;

            var prevSpline = GenerateRoadEdge(nodes[0], nodes[nodes.Length - 1], -1).Inverse();

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
                    var innerSpline = GenerateRoadEdge(startNode, endNode, -1);
                    var outerSpline = GenerateRoadEdge(startNode, endNode, 1);

                    //Calculations for the fill patch
                    topSpline = prevSpline;
                    bottomSpline = outerSpline;
                    leftSpline = preToStartSpline;
                    rightSpline = nextToEndSpline;


                    //Draw the road strip with vertical offset
                    GenerateIntersectionStrip(mesh, startNode, endNode, accuracy, voffset);

                    prevSpline = innerSpline;
                }
                topSpline = topSpline.Inverse();

                if (DebugOptions.DebugSectionFences) {
                    //Debug fences
                    var h = Vector3.UnitY * 5;
                    RenderPatch.DrawDebugFence(mesh, topSpline, h, Color.Red);
                    RenderPatch.DrawDebugFence(mesh, bottomSpline, h, Color.Maroon);
                    RenderPatch.DrawDebugFence(mesh, leftSpline, h, Color.Lime);
                    RenderPatch.DrawDebugFence(mesh, rightSpline, h, Color.Green);
                }

                //Render the last-node or the inter-strip patch with vertical offset
                var offset = new Vector3(0, voffset, 0);
                RenderPatch.RenderCoonsPatch(mesh, bottomSpline, topSpline, leftSpline.Inverse(), rightSpline.Inverse(), (p, uv) => CreateVertex(p + offset, color), accuracy, accuracy);

                lbound++;
                ubound--;
            }
        }

        internal static void GenerateSectionMesh(RoadSection roadSection, MultiMesh multimesh, int accuracy = 17, float voffset = 0.01f) {
            var mesh = multimesh.GetOrCreateRenderBinForced(Assets.Asphalt);

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
            while (rightNode != endNode) {
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
            GenerateIntersectionStrip(mesh, start, end, accuracy, voffset);


            //Generate other vertices on the right
            GenerateSubrangeVerts(mesh, rightNodes.ToArray(), 1, accuracy, voffset);


            //Generate other vertices on the left
            GenerateSubrangeVerts(mesh, leftNodes.ToArray(), -1, accuracy, voffset);

            mesh.AddTagsToLastTriangles(-1, roadSection);
        }
    }
}