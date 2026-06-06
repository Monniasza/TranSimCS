using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        public static void GenerateIntersectionStrip(Mesh mesh, RoadNodeEnd start, RoadNodeEnd end, int accuracy = 17) {
            //Generate bounding edges
            var startLeft = calcBoundingLineEndFaced(start, -1);
            var startRight = calcBoundingLineEndFaced(start, 1);
            var endRight = calcBoundingLineEndFaced(end, 1);
            var endLeft = calcBoundingLineEndFaced(end, -1);

            var leftEdge = GenerateSplinePoints(startLeft.Position, endRight.Position, startLeft.Tangential, endRight.Tangential, accuracy);
            var rightEdge = GenerateSplinePoints(startRight.Position, endLeft.Position, startRight.Tangential, endLeft.Tangential, accuracy);
            var wovenStrip = WeaveStrip(leftEdge, rightEdge);

            // Apply vertical offset to prevent Z-fighting with ground
            var stripVerts = wovenStrip.Select(CreateVertex).ToArray();

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

        public static void GenerateSubrangeVerts(Mesh mesh, RoadNodeEnd[] nodes, int discriminant, int accuracy = 17) {
            var lbound = 1;
            var ubound = nodes.Length - 2;

            var prevSpline = GenerateRoadEdge(nodes[0], nodes[^1], -1).Inverse();

            while (lbound <= ubound) {
                var preNode = nodes[lbound - 1];
                var startNode = nodes[lbound];
                var endNode = nodes[ubound];
                var nextNode = nodes[ubound + 1];
                var color = Color.White;

                var preToStartSpline = GenerateRoadEdge(preNode, startNode, -1);
                var nextToEndSpline = GenerateRoadEdge(nextNode, endNode, 1);

                ISpline<Vector3> bottomSpline;
                var leftSpline = preToStartSpline;
                var rightSpline = nextToEndSpline;
                var topSpline = prevSpline;

                if (lbound == ubound) { //One node remaining
                    var bounds = startNode.Bounds();
                    var lpos = calcLineEnd(startNode, bounds.LocalLeft).Position;
                    var rpos = calcLineEnd(startNode, bounds.LocalRight).Position;
                    bottomSpline = new LineSegment(rpos, lpos);
                } else { //More nodes remaining
                    var innerSpline = GenerateRoadEdge(startNode, endNode, -1);
                    var outerSpline = GenerateRoadEdge(startNode, endNode, 1);

                    //Calculations for the fill patch
                    bottomSpline = outerSpline;
                    prevSpline = innerSpline;

                    //Draw the road strip with vertical offset
                    GenerateIntersectionStrip(mesh, startNode, endNode, accuracy);
                }
                topSpline = topSpline.Inverse();

                //Render the last-node or the inter-strip patch with vertical offset
                RenderPatch.RenderCoonsPatch(mesh, bottomSpline, topSpline, leftSpline.Inverse(), rightSpline.Inverse(), (p, uv) => CreateVertex(p, color), accuracy, accuracy);

                lbound++; ubound--;
            }
        }

        internal static void GenerateSectionMesh(RoadSection roadSection, MultiMesh multimesh, int accuracy = 17) {
            if (roadSection.Nodes.Count < 1) return; //Guard agains empty sections

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
            var rightNodes = CollectNodes(startNode, endNode);
            var leftNodes = CollectNodes(endNode, startNode);

            //Generate the main strip with vertical offset to prevent Z-fighting
            GenerateIntersectionStrip(mesh, start, end, accuracy);
            //Generate other vertices on the right
            GenerateSubrangeVerts(mesh, rightNodes.ToArray(), 1, accuracy);
            //Generate other vertices on the left
            GenerateSubrangeVerts(mesh, leftNodes.ToArray(), -1, accuracy);

            mesh.AddTagsToLastTriangles(-1, roadSection);
        }

        private static List<RoadNodeEnd> CollectNodes(DLNode<RoadNodeEnd> from, DLNode<RoadNodeEnd> to) {
            var result = new List<RoadNodeEnd>();
            var i = from;
            while (i != to) {
                result.Add(i.val);
                i = i.Next;
            }
            result.Add(i.val);
            return result;
        }
    }
}