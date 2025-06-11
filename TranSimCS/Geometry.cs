using System;
using System.Collections.Generic;
using System.Linq;
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
            public float Radius { get; }
            public LineEnd(Vector3 position, Vector3 tangential, Vector3 normal, Vector3 lateral, float radius) {
                Position = position;
                Tangential = tangential;
                Normal = normal;
                Lateral = lateral;
                Radius = radius;
            }
        }

        /// <summary>
        /// Calculates the end position of a line segment given a starting position, an offset, and an angle.
        /// </summary>
        /// <param name="nodePos">road node position</param>
        /// <param name="offset">Offset from the centerline</param>
        /// <param name="angle">Angle in the 2^32 field</param>
        /// <returns></returns>
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
            float radius = (1f / node.HCurvature) - offset; // Assuming offset is the radius of curvature
            Vector3 position = new Vector3(x, nodePos.Y, z); // Position of the end point

            return new LineEnd(position, tangential, normal, lateral, radius); // Return the end position as a Vector3
        }
        
        public static Bezier3 GenerateJoinSpline(Vector3 startPos, Vector3 endPos, Vector3 startTangent, Vector3 endTangent){
            float tangentLength = Vector3.Distance(startPos, endPos) * 0.5f;
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
    }
    public struct Bezier3 {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        public Vector3 d;

        public Bezier3(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }
        public Bezier3(Vector3 a) {
            this.a = a;
            this.b = a;
            this.c = a;
            this.d = a;
        }


        public Vector3 this[float t] {
            get {
                float u = 1 - t;
                return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
            }
        }

        public static Bezier3 operator +(Bezier3 first, Bezier3 other) {
            return new Bezier3 {
                a = first.a + other.a,
                b = first.b + other.b,
                c = first.c + other.c,
                d = first.d + other.d
            };
        }

        public static Bezier3 operator +(Bezier3 first, Vector3 other) {
            return new Bezier3 {
                a = first.a + other,
                b = first.b + other,
                c = first.c + other,
                d = first.d + other
            };
        }

        public static Bezier3 operator -(Bezier3 first, Bezier3 other) {
            return new Bezier3 {
                a = first.a - other.a,
                b = first.b - other.b,
                c = first.c - other.c,
                d = first.d - other.d
            };
        }

        public static Bezier3 operator -(Bezier3 first, Vector3 other) {
            return new Bezier3 {
                a = first.a - other,
                b = first.b - other,
                c = first.c - other,
                d = first.d - other
            };
        }
        public static Bezier3 operator *(Bezier3 bezier, float scalar) {
            return new Bezier3 {
                a = bezier.a * scalar,
                b = bezier.b * scalar,
                c = bezier.c * scalar,
                d = bezier.d * scalar
            };
        }
        public static Bezier3 operator /(Bezier3 bezier, float scalar) {
            if (scalar == 0) throw new DivideByZeroException("Cannot divide by zero.");
            return new Bezier3 {
                a = bezier.a / scalar,
                b = bezier.b / scalar,
                c = bezier.c / scalar,
                d = bezier.d / scalar
            };
        }

        public static Bezier3 SubSection(Bezier3 bezier, float startT, float endT) {
            if (startT < 0 || endT > 1 || startT >= endT) throw new ArgumentOutOfRangeException("startT and endT must be in the range [0, 1] and startT < endT.");
            Split(bezier, startT, out var beginningSection, out var endSection);
            Split(endSection, (endT - startT) / (1 - startT), out var finalStart, out var finalEnd);
            return finalStart;
        }
        public static void TriSection(Bezier3 bezier, float startT, float endT, out Bezier3 startSection, out Bezier3 midSection, out Bezier3 endSection) {
            if (startT < 0 || endT > 1 || startT >= endT) throw new ArgumentOutOfRangeException("startT and endT must be in the range [0, 1] and startT < endT.");
            if(startT == 0 && endT == 1) {
                startSection = new Bezier3(bezier.a);
                midSection = bezier;
                endSection = new Bezier3(bezier.d);
                return;
            }else if(startT == 0) {
                startSection = new Bezier3(bezier.a);
                Split(bezier, endT, out midSection, out endSection);
                return;
            } else if(endT == 1) {
                Split(bezier, startT, out startSection, out midSection);
                endSection = new Bezier3(bezier.d);
                return;
            }

            Split(bezier, startT, out startSection, out midSection);
            Split(midSection, (endT - startT) / (1 - startT), out var finalStart, out endSection);
        }

        public static void Split(Bezier3 source, float t, out Bezier3 start, out Bezier3 end) {
            var ab = Vector3.Lerp(source.a, source.b, t);
            var bc = Vector3.Lerp(source.b, source.c, t);
            var cd = Vector3.Lerp(source.c, source.d, t);
            var abc = Vector3.Lerp(ab, bc, t);
            var bcd = Vector3.Lerp(bc, cd, t);
            var abcd = Vector3.Lerp(abc, bcd, t);

            start.a = source.a;
            start.b = ab;
            start.c = abc;
            start.d = abcd;
            end.a = abcd;
            end.b = bcd;
            end.c = cd;
            end.d = source.d;
        }

        public static void SplitInHalf(Bezier3 source, out Bezier3 start, out Bezier3 end) {
            var ab = (source.a + source.b) / 2;
            var bc = (source.b + source.c) / 2;
            var cd = (source.c + source.d) / 2;
            var abc = (ab + bc) / 2;
            var bcd = (bc + cd) / 2;
            var abcd = (abc + bcd) / 2;

            start.a = source.a;
            start.b = ab;
            start.c = abc;
            start.d = abcd;
            end.a = abcd;
            end.b = bcd;
            end.c = cd;
            end.d = source.d;
        }

        /// <summary>
        /// Finds the t value on the spline that is closest to the given position.
        /// </summary>
        /// <param name="spline">source spline</param>
        /// <param name="pos">target point to find the nearest T to</param>
        /// <param name="accuracy">number of loop iterations</param>
        /// <param name="depth">number of iterations to find the closest point</param>
        /// <param name="lowerLimit">lower limit of the t value</param>
        /// <param name="upperLimit">upper limit of the t value</param>
        /// <param name="pointsPerCycle">number of points to generate per cycle</param>
        /// <returns></returns>
        public static float FindT(Bezier3 spline, Vector3 pos, int pointsPerCycle = 20, int depth = 5, float lowerLimit = 0, float upperLimit = 1) {
            if(depth <= 0) {
                return (lowerLimit + upperLimit) / 2f; // Return the average of the bounds if depth is exhausted
            }
            var points = Geometry.GenerateSplinePoints(spline, pointsPerCycle, lowerLimit, upperLimit);
            int closestIndex = 0;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < points.Length; i++) {
                float distance = Vector3.Distance(points[i], pos);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }
            int minIndex = Math.Max(0, closestIndex - 2);
            int maxIndex = Math.Min(points.Length - 1, closestIndex + 2);
            float minT = minIndex / (float)(points.Length - 1);
            float maxT = maxIndex / (float)(points.Length - 1);
            minT = MathHelper.Lerp(lowerLimit, upperLimit, minT); // Scale minT to the original range
            maxT = MathHelper.Lerp(lowerLimit, upperLimit, maxT); // Scale maxT to the original range
            return FindT(spline, pos, pointsPerCycle, depth - 1, minT, maxT); // Recursively find the closest t value in the range
        }
    }
}
