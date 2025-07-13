using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Roads;

namespace TranSimCS
{
    internal static class Geometry
    {
        /// <summary>
        /// Calculates the distance between two points in 2D space.
        /// </summary>
        /// <param name="x1">X coordinate of the first point.</param>
        /// <param name="y1">Y coordinate of the first point.</param>
        /// <param name="x2">X coordinate of the second point.</param>
        /// <param name="y2">Y coordinate of the second point.</param>
        /// <returns>The distance between the two points.</returns>
        public static float Distance(float x1, float y1, float x2, float y2)
        {
            return MathF.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        }

        public readonly struct LineEnd{
            public Vector3 Position { get; }
            public Vector3 Tangential { get; }
            public Vector3 Normal { get; }
            public Vector3 Lateral { get; }
            public LineEnd(Vector3 position, Vector3 tangential, Vector3 normal, Vector3 lateral) {
                Position = position;
                Tangential = tangential;
                Normal = normal;
                Lateral = lateral;
            }
        }

        public static LineEnd calcLineEnd(RoadNodeEnd node, float offset)
            => calcLineEnd(node.Node, offset, node.End);

        public static LineEnd calcLineEnd(RoadNode node, float offset, NodeEnd end) {
            Transform3 nodeTransform = node.Position.Value.CalcReferenceFrame();
            Vector3 nodePosition = nodeTransform.O;
            Vector3 tangential = nodeTransform.Z;
            Vector3 normal = nodeTransform.Y;
            Vector3 lateral = nodeTransform.X;
            Vector3 position = nodePosition + lateral * offset;
            if(end == NodeEnd.Backward) {
                tangential = -tangential;
                lateral = -lateral;
            }
            return new LineEnd(position, tangential, normal, lateral); // Return the end position as a Vector3
        }

        public static Bezier3 GenerateJoinSpline(Vector3 startPos, Vector3 endPos, Vector3 startTangent, Vector3 endTangent){
            float tangentLength = Vector3.Distance(startPos, endPos) * 0.4f;
            Vector3 a = startPos;
            Vector3 b = startPos + startTangent * tangentLength; // Start tangent point
            Vector3 c = endPos - endTangent * tangentLength; // End tangent point
            Vector3 d = endPos; // End position
            return new Bezier3 { a = a, b = b, c = c, d = d };
        }

        public static Vector3[] GenerateSplinePoints(Bezier3 spline, int numPoints = 32, float minT = 0, float maxT = 1) {
            if (numPoints < 2) throw new ArgumentException("numPoints must be at least 2.");
            Vector3[] points = new Vector3[numPoints];
            float step = 1f / (numPoints - 1);
            Bezier3 bezier = spline; // Use the provided Bezier curve
            for (int i = 0; i < numPoints; i++) {
                float t = i * step;
                points[i] = bezier[MathHelper.Lerp(minT, maxT, t)]; // Use the Bezier curve to calculate the point at t
            }
            return points;
        }

        public static Vector3[] GenerateSplinePoints(Vector3 startPos, Vector3 endPos, Vector3 startTangent, Vector3 endTangent, int numPoints = 32)
        {
            return GenerateSplinePoints(GenerateJoinSpline(startPos, endPos, startTangent, endTangent), numPoints);
        }

        public static VertexPositionColorTexture[] GeneratePositionsFromVectors(float xPos, Color color, params Vector3[] vectors)
        {
            var positions = new VertexPositionColorTexture[vectors.Length];
            var step = 1f / (vectors.Length - 1);
            for (int i = 0; i < vectors.Length; i++)
            {
                positions[i] = new VertexPositionColorTexture(vectors[i], color, new Vector2(xPos, step*i));
            }
            return positions;
        }

        public static T[] WeaveStrip<T>(IEnumerable<T> l, IEnumerable<T> r) {
            var iterL = l.GetEnumerator();
            var iterR = r.GetEnumerator();
            var results = new List<T>();
            while (true) {
                if (!iterL.MoveNext()) break;
                results.Add(iterL.Current);
                if (!iterR.MoveNext()) break;
                results.Add(iterR.Current);
            }
            return results.ToArray();
        }

        public static bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float intersectionDistance) {
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 h = Vector3.Cross(ray.Direction, edge2);
            float a = Vector3.Dot(edge1, h);
            intersectionDistance = float.MaxValue; // Default value in case of no intersection
            if (MathF.Abs(a) < 1e-6f) // Check if the ray is parallel to the triangle
                return false; // No intersection
            float f = 1.0f / a;
            Vector3 s = ray.Position - v0;
            float u = f * Vector3.Dot(s, h);
            if (u < 0.0f || u > 1.0f) // Check if the intersection is outside the triangle
                return false; // No intersection
            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(ray.Direction, q);
            if (v < 0.0f || u + v > 1.0f) // Check if the intersection is outside the triangle
            {
                return false; // No intersection
            }
            // Calculate the intersection point
            float t = f * Vector3.Dot(edge2, q);
            if (t > 1e-6f) // Check if the intersection is in front of the ray
            {
                intersectionDistance = t; // Calculate the intersection point
                return true; // Intersection found
            }
            return false; // No intersection, the triangle is behind the ray

        }

        public static float IntersectRayPlaneT(Ray ray, Plane plane) =>
            -(Vector3.Dot(ray.Position, plane.Normal) + plane.D) / (Vector3.Dot(ray.Direction, plane.Normal));
        
        public static Vector3 IntersectRayPlane(Ray ray, Plane plane) {
            var t = IntersectRayPlaneT(ray, plane);
            return ray.Position + (t * ray.Direction);
        }
        public static Vector3 ReflectVectorByNormal(Vector3 src, Vector3 normal) => src - 2 * Vector3.Dot(src, normal) * normal;


        public static VertexPositionColorTexture OffsetVert(VertexPositionColorTexture vert, Vector3 offset) {
            return new VertexPositionColorTexture(vert.Position + offset, vert.Color, vert.TextureCoordinate);
        }
        public static VertexPositionColorTexture SubVert(VertexPositionColorTexture vert, Vector3 offset) {
            return new VertexPositionColorTexture(vert.Position - offset, vert.Color, vert.TextureCoordinate);
        }

        public static float FieldToRadians(int azimuth) {
            return (azimuth / (float)(1L << 32)) * MathF.PI * 2;
        }
        public static int RadiansToField(float azimuthRadians) {
            return (int)MathF.Round(azimuthRadians * (float)(1L << 32) / MathF.Tau);
        }

        public static Vector2 RoadEndToRange(NodeEnd end) {
            if (end == NodeEnd.Forward) return new(0, 1);
            if(end == NodeEnd.Backward) return new(-1, 0);
            throw new ArgumentException("Invalid node end");
        }

        public static float hypot2(float x, float z) {
            return MathF.Sqrt(x * x + z * z);
        }
        public static float hypot2sqr(float x, float z) {
            return (x * x + z * z);
        }

        
    }
}
