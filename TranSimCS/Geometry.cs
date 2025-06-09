using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        /// <summary>
        /// Calculates the end position of a line segment given a starting position, an offset, and an angle.
        /// </summary>
        /// <param name="nodePos">road node position</param>
        /// <param name="offset">Offset from the centerline</param>
        /// <param name="angle">Angle in the 2^32 field</param>
        /// <returns></returns>
        public static Vector3 calcLineEnd(Vector3 nodePos, float offset, int angle)
        {
            float radians = (angle / (float)(1L << 32)) * MathF.PI * 2; // Convert angle to radians
            float x = nodePos.X + offset * MathF.Cos(radians);
            float z = nodePos.Z - offset * MathF.Sin(radians);
            return new Vector3(x, nodePos.Y, z); // Return the end position as a Vector3
        }

        public readonly struct LineEnd{
            public Vector3 Position { get; }
            public Vector3 Tangential { get; }
            public Vector3 Normal { get; }
            public Vector3 Lateral { get; }
            public float Radius { get; }
            public LineEnd(Vector3 position, Vector3 tangential, Vector3 normal, Vector3 lateral, float radius) {
                Position = position;
                Tangential = tangential;
                Normal = normal;
                Lateral = lateral;
                Radius = radius;
            }
        }
        public static LineEnd calcLineEnd2(RoadNode node, int laneIndex) {
            float offset = node.PositionOffsets[laneIndex];
            Vector3 nodePos = node.Position;
            int angle = node.Azimuth; // Azimuth angle in the 2^32 field
            float radians = (angle / (float)(1L << 32)) * MathF.PI * 2; // Convert angle to radians
            float sine = MathF.Sin(radians);
            float cosine = MathF.Cos(radians);
            float x = nodePos.X + offset * cosine;
            float z = nodePos.Z - offset * sine;

            Vector3 tangential = new Vector3(sine, 0, cosine); // Tangential vector (along the road)
            Vector3 normal = new Vector3(0, 1, 0); // Normal vector (upwards)
            Vector3 lateral = new Vector3(cosine, 0, -sine); // Lateral vector (to the right)
            float radius = (1f / node.Curvature) - offset; // Assuming offset is the radius of curvature
            Vector3 position = new Vector3(x, nodePos.Y, z); // Position of the end point

            return new LineEnd(position, tangential, normal, lateral, radius); // Return the end position as a Vector3
        }

        public static Vector3 calcLineEnd(RoadNode node, int laneIndex)  => calcLineEnd(node.Position, node.PositionOffsets[laneIndex], node.Azimuth);
    
        public struct Bezier3 {
            public Vector3 a;
            public Vector3 b;
            public Vector3 c;
            public Vector3 d;
            public Vector3 this[float t] {
                get {
                    float u = 1 - t;
                    return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
                }
            }
        }
        public static Bezier3 GenerateJoinSpline(Vector3 startPos, Vector3 endPos, Vector3 startTangent, Vector3 endTangent){
            float tangentLength = Vector3.Distance(startPos, endPos) * 0.5f;
            Vector3 a = startPos;
            Vector3 b = startPos + startTangent * tangentLength; // Start tangent point
            Vector3 c = endPos - endTangent * tangentLength; // End tangent point
            Vector3 d = endPos; // End position
            return new Bezier3 { a = a, b = b, c = c, d = d };
        }

        public static Vector3[] GenerateSplinePoints(Vector3 startPos, Vector3 endPos, Vector3 startTangent, Vector3 endTangent, int numPoints = 32)
        {
            if (numPoints < 2) throw new ArgumentException("numPoints must be at least 2.");
            Vector3[] points = new Vector3[numPoints];
            float step = 1f / (numPoints - 1);
            float tangentLength = Vector3.Distance(startPos, endPos) * 2;
            Bezier3 bezier = GenerateJoinSpline(startPos, endPos, startTangent, endTangent);
            for (int i = 0; i < numPoints; i++)
            {
                float t = i * step;
                points[i] = bezier[t]; // Use the Bezier curve to calculate the point at t
            }
            return points;
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
    }
}
