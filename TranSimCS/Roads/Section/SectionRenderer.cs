using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NLog;
using TranSimCS.Debugging;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.Render;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Setting;
using TranSimCS.Spline;
using static TranSimCS.Geometry.GeometryUtils;
using static TranSimCS.Geometry.LineEnd;

namespace TranSimCS.Roads.Section {
    internal static class SectionRenderer {

        private static Logger logger = LogManager.GetCurrentClassLogger();

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

        private static void GenerateSectionBySlope(Mesh surfaceMesh, RoadSection roadSection, RoadNodeEnd start, RoadNodeEnd end, int accuracy = 17) {
            //Rotate the list so the 1st main end lies on the index 0
            var circularList = DLNode<RoadNodeEnd>.CreateCircular(roadSection.Nodes);
            var startNode = circularList;
            while (startNode.val != start) startNode = startNode.Next;
            var endNode = circularList;
            while (endNode.val != end) endNode = endNode.Next;

            //Categorize the nodes into categories: left or right of the main. Since they're already sorted, there's no need to sort.
            var rightNodes = CollectNodes(startNode, endNode);
            var leftNodes = CollectNodes(endNode, startNode);

            //Generate the main strip with vertical offset to prevent Z-fighting
            GenerateIntersectionStrip(surfaceMesh, start, end, accuracy);
            GenerateSectionWithoutSlope(surfaceMesh, leftNodes.ToArray(), accuracy);
            GenerateSectionWithoutSlope(surfaceMesh, rightNodes.ToArray(), accuracy);
        }

        private static void GenerateSectionWithoutSlope(Mesh surfaceMesh, RoadSection roadSection, int accuracy = 17)
            => GenerateSectionWithoutSlope(surfaceMesh, roadSection.Nodes.ToArray(), accuracy);
        private static void GenerateSectionWithoutSlope(Mesh surfaceMesh, RoadNodeEnd[] nodes, int accuracy) {
            if(nodes.Length == 0) return;
            Vector3 center = Vector3.Zero;
            for (int i = 0; i < nodes.Length; i++) center += nodes[i].CenterPosition;
            center /= nodes.Length;
            var perimeter = GenerateSectionPerimeter(nodes, accuracy);
            surfaceMesh.DrawCenteredPoly(CreateVertex(center), perimeter.Select(CreateVertex).ToArray());
        }

        internal static void GenerateSectionMesh(RoadSection roadSection, MultiMesh multimesh, int accuracy = -1) {
            if (roadSection.Nodes.Count < 1) return; //Guard agains empty sections

            if (accuracy < 0) accuracy = Settings.RoadAccuracy;

            var mesh = multimesh.GetOrCreateRenderBinForced(Assets.Asphalt);
            var surfaceMesh = new Mesh();

            var endsPair = roadSection.MainSlopeNodes.Value;
            var hasSlope = endsPair.Start != null
                && endsPair.End != null
                && endsPair.Start != endsPair.End
                && roadSection.Nodes.Contains(endsPair.Start)
                && roadSection.Nodes.Contains(endsPair.End);

            if (hasSlope) {
                GenerateSectionBySlope(surfaceMesh, roadSection, endsPair.Start, endsPair.End, accuracy);
            } else if (roadSection.Nodes.Count > 2) {
                GenerateSectionWithoutSlope(surfaceMesh, roadSection, accuracy);
            } else if (roadSection.Nodes.Count == 2) {
                GenerateIntersectionStrip(surfaceMesh, roadSection.Nodes[0], roadSection.Nodes[1], accuracy);
            } else {
                GenerateSectionWithoutSlope(surfaceMesh, roadSection, accuracy);
            }

            mesh.DrawModel(surfaceMesh);
            mesh.AddTagsToLastTriangles(-1, roadSection);

            GenerateSectionFinish(roadSection, multimesh, accuracy);
        }

        

        private static void GenerateSectionFinish(RoadSection roadSection, MultiMesh multimesh, int accuracy = 17) {
            var finish = roadSection.Finish;
            var texture = finish.subsurface.GetTexture();
            if (texture == null || finish.depth <= 0) return;

            var normal = roadSection.Normal;
            if (normal.LengthSquared() < 1e-6f) normal = Vector3.Up;
            else normal.Normalize();

            var height = finish.depth;
            var breadth = finish.depth * MathF.Tan(finish.angle);

            var sideLen = new Vector2(height, breadth).Length();
            var topVertexer = UniformTexturing.WithFixedU(0);
            var bottomVertexer = UniformTexturing.WithFixedU(sideLen);
            var finishMesh = multimesh.GetOrCreateRenderBinForced(texture);

            //Generate the splines
            var splineCount = roadSection.Nodes.Count;
            var perimeterPointCount = splineCount * accuracy;
            for(int i = 0; i < splineCount; i++) {
                var h = (i + 1) % splineCount;
                var prev = roadSection.Nodes[i];
                var next = roadSection.Nodes[h];

                var topSpline = GenerateRoadEdge(next, prev, 1);
                var topPoints = GeometryUtils.GenerateSplinePoints(topSpline, accuracy);

                //Generate bottom points
                var bottomPoints = new Vector3[accuracy];
                logger.Info($"Generating edge {i} for a road section");
                for (int j = 0; j < accuracy; j++) {
                    var amount = j / (accuracy - 1f);
                    var tangent = topSpline.Tangential(amount);
                    var lateral = Vector3.Cross(normal, tangent).Normalized();
                    bottomPoints[j] = topPoints[j] + (lateral * breadth) - (normal * height);
                }
                
                //THE PROBLEM: If a bend is tight, the bottom spline inverts and goes backwards

                var generatedSplines = UniformTexturing.UniformTexturedTwin(topPoints, bottomPoints, UniformTexturing.PairStrip(0, sideLen, Color.White));
                finishMesh.DrawStrip(generatedSplines);
            }

            //Generate endcaps
            for (int i = 0; i < splineCount; i++) {
                var node = roadSection.Nodes[i];
                var refframe = node.Node.ReferenceFrame;
                var bounds = node.Bounds();
                var mulbreadth = breadth;
                if (node.End == NodeEnd.Backward) mulbreadth *= -1;
                var p0 = refframe.O + refframe.X * bounds.LocalLeft;
                var p1 = refframe.O + refframe.X * bounds.LocalRight;
                var p2 = refframe.O + refframe.X * (bounds.LocalRight + mulbreadth) - refframe.Y * height;
                var p3 = refframe.O + refframe.X * (bounds.LocalLeft - mulbreadth) - refframe.Y * height;
                var u0 = new Vector2(bounds.Min, 0);
                var u1 = new Vector2(bounds.Max, 0);
                var u2 = new Vector2(bounds.Max + breadth, height);
                var u3 = new Vector2(bounds.Min - breadth, height);
                VertexPositionColorTexture v0 = new(p0, Color.White, u0);
                VertexPositionColorTexture v1 = new(p1, Color.White, u1);
                VertexPositionColorTexture v2 = new(p2, Color.White, u2);
                VertexPositionColorTexture v3 = new(p3, Color.White, u3);
                finishMesh.DrawQuad(v0, v1, v2, v3);
            }
        }

        private static Vector3[] GenerateSectionPerimeter(RoadSection roadSection, int accuracy = 17)
            => GenerateSectionPerimeter(roadSection.Nodes.ToArray(), accuracy);
        private static Vector3[] GenerateSectionPerimeter(RoadNodeEnd[] nodes, int accuracy) {
            var perimeter = new List<Vector3>();

            for (int i = 0; i < nodes.Length; i++) {
                var node = nodes[i];
                var next = nodes[(i + 1) % nodes.Length];
                var bounds = node.Bounds();
                var left = calcLineEnd(node, bounds.LocalLeft).Position;
                var right = calcLineEnd(node, bounds.LocalRight).Position;

                if (perimeter.Count == 0)
                    perimeter.Add(left);
                perimeter.Add(right);

                var edge = GenerateRoadEdge(node, next, 1);
                var points = GenerateSplinePoints(edge, accuracy);
                var end = i == nodes.Length - 1 ? points.Length - 1 : points.Length;
                for (int j = 1; j < end; j++)
                    perimeter.Add(points[j]);
            }
            return perimeter.ToArray();
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
