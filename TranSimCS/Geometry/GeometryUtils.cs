using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using TranSimCS.Roads.Node;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Geometry
{    public static partial class GeometryUtils
    {
        public static Vector3 FindNearest(Ray ray, Vector3 point, out float tt) {
            var direction = ray.Direction;
            var dirLen = direction.Length();
            direction.Normalize();
            var point2vec = point - ray.Position;
            var dist = Vector3.Dot(direction, point2vec);
            var t = dist / dirLen;
            tt = t;
            return ray.Position + t * ray.Direction;
        }

        /// <summary>
        /// Calculates the distance between two points in 2D space.
        /// </summary>
        /// <param name="x1">X coordinate of the first point.</param>
        /// <param name="y1">Y coordinate of the first point.</param>
        /// <param name="x2">X coordinate of the second point.</param>
        /// <param name="y2">Y coordinate of the second point.</param>
        /// <returns>The distance between the two points.</returns>
        public static float Distance(float x1, float y1, float x2, float y2) => MathF.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));     

        public static Bezier3 GenerateJoinSpline(Ray start, Ray end) => GenerateJoinSpline(start.Position, end.Position, start.Direction, end.Direction);

        //Coefficients
        const float bezierAtCollinear = 0.333f;
        const float bezierAtRight = 0.390f;
        const float bezierAtUTurn = 0.666f;
        const float coeff0 = bezierAtRight;
        //coeff2-coeff1+coeff0 = bezierAtCollinear;
        //coeff2+coeff1+coeff0 = bezierAtUTurn;
        //2*coeff1 = bezierAtUTurn - bezierAtCollinear;
        const float coeff1 = (bezierAtUTurn - bezierAtCollinear) / 2;
        //coeff2 + (bezierAtUTurn - bezierAtCollinear)/2 + bezierAtRight = bezierAtUTurn
        //coeff2 + bezierAtUTurn/2 - bezierAtCollinear/2 + bezierAtRight = bezierAtUTurn
        //bezierAtUTurn/2 - bezierAtCollinear/2 + bezierAtRight = bezierAtUTurn - coeff2
        //bezierAtUTurn/2 - bezierAtCollinear/2 + bezierAtRight - bezierAtUTurn = -coeff2
        //-bezierAtUTurn/2 - bezierAtCollinear/2 + bezierAtRight = -coeff2
        const float coeff2 = bezierAtUTurn / 2 + bezierAtCollinear / 2 - bezierAtRight;

        public static Bezier3 GenerateJoinSpline(Vector3 startPos, Vector3 endPos, Vector3 startTangent, Vector3 endTangent) {
            float chord = Vector3.Distance(startPos, endPos);

            float dot = Vector3.Dot(startTangent, endTangent) / (startTangent.Length() * endTangent.Length());

            float calculatedMagnitude = (dot * dot * coeff2) + (dot * coeff1) + coeff0;

            float tangentLength = chord * calculatedMagnitude;

            Vector3 a = startPos;
            Vector3 b = startPos + startTangent * tangentLength; // Start tangent point
            Vector3 c = endPos + endTangent * tangentLength; // End tangent point
            Vector3 d = endPos; // End position
            return new Bezier3 { a = a, b = b, c = c, d = d };
        }

        public static Vector3[] GenerateSplinePoints(ISpline<Vector3> spline, int numPoints = 32, float minT = 0, float maxT = 1) {
            if (numPoints < 2) throw new ArgumentException("numPoints must be at least 2.");
            Vector3[] points = new Vector3[numPoints];
            float step = 1f / (numPoints - 1);
            // Use the provided Bezier curve
            for (int i = 0; i < numPoints; i++) {
                float t = i * step;
                points[i] = spline[MathHelper.Lerp(minT, maxT, t)]; // Use the Bezier curve to calculate the point at t
            }
            return points;
        }

        public static Vector3[] GenerateSplinePoints(Vector3 startPos, Vector3 endPos, Vector3 startTangent, Vector3 endTangent, int numPoints = 32)
            => GenerateSplinePoints(GenerateJoinSpline(startPos, endPos, startTangent, endTangent), numPoints);

        public static float CountLength(Vector3[] points) {
            float length = 0;
            for (int i = 1; i < points.Length; i++) {
                var prev = points[i - 1];
                var next = points[i];
                length += Vector3.Distance(prev, next);
            }
            return length;
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

        public static bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float intersectionDistance, float minT = 1e-6f, float maxT = float.PositiveInfinity) {
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
            if (t > minT && t < maxT) // Check if the intersection is in front of the ray
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
        public static VertexPositionColorTexture CreateVertex(Vector3 pos) {
            return new(pos, Color.White, new(pos.X, pos.Z));
        }

        public static VertexPositionColorTexture CreateVertex(Vector3 pos, Color c) {
            return new(pos, c, new(pos.X, pos.Z));
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
        public static double hypot2sqr(double x, double z) {
            return (x * x + z * z);
        }

        //NORMAL CALCULATIONS
        /// <summary>
        /// Normal vector for an arbitrary polygon. Works even if the polygon is not flat.
        /// </summary>
        /// <param name="vertices">list of vertices clockwise</param>
        /// <returns></returns>
        public static Vector3 NormalPoly(params Vector3[] vertices) {
            Vector3 crossSum = new Vector3();
            for (int i = 0; i < vertices.Length; i++) {
                var v1 = vertices[i];
                var v2 = vertices[(i + 1) % vertices.Length];
                crossSum += Vector3.Cross(v1, v2);
            }
            crossSum.Normalize();
            return crossSum;
        }

        /// <summary>
        /// Compares two vectors based on their relative direction in respect of a normal rather than individual components or their lengths.
        /// <br>returns 0 if A or B is equal to 0
        /// <br>returns 0 is A and B are on the same line
        /// <br>returns + if A is clockwise of B in respect to the normal
        /// <br>returns - otherwise
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="n"></param>
        /// <returns>negative if A is counter-clockwise of B, positive if clockwise and 0 if on the same line or any vector is 0</returns>
        public static int CompareRotary(Vector3 a, Vector3 b, Vector3 n) {
            Vector3 c = n * a;
            var discriminant = Vector3.Dot(c, b);
            var reference = 0.0f;
            return discriminant.CompareTo(reference);
        }

        public static readonly float fieldsInDeg = MathF.Pow(2.0f, 32) / 360f;
        public static readonly float degsInField = 360f / MathF.Pow(2.0f, 32);

        public static float FieldToDegs(int azimuth) {
            return (float)azimuth * degsInField;
        }
        public static int DegsToField(float v) {
            return (int)(v * fieldsInDeg);
        }

        public static Vector3 Normalized(this Vector3 v) {
            var result = v;
            result.Normalize();
            return result;
        }
        public static Vector3 Orthogonalize(this Vector3 v, Vector3 n) {
            float nn = Vector3.Dot(n, n);
            const float eps = 1e-8f;

            if (nn < eps)
                return v;

            // Normalize only if magnitude is extreme
            float invLen = 1.0f / MathF.Sqrt(nn);
            Vector3 nNorm = n * invLen;

            return v - Vector3.Dot(v, nNorm) * nNorm;
        }

        public static float UnLerp(float start, float end, float point) {
            return (start - point) / (start - end);
        }

        public static float CosBetweenLines(Vector3 start, Vector3 mid, Vector3 end) {
            var a = mid - start;
            var b = end - mid;
            return Vector3.Dot(a, b) / MathF.Sqrt(a.LengthSquared() * b.LengthSquared());
        }
        public static float DistanceToLine(Vector3 start, Vector3 end, Vector3 point) {
            var vector = end - start;
            var toPoint = point - start;
            return Vector3.Cross(vector, toPoint).Length() / vector.Length();
        }
        public static Quaternion QuaternionFromBasisVectors(Vector3 X, Vector3 Y, Vector3 Z) {
            var T = X.X + Y.Y + Z.Z;
            if(T > 0) {
                //Case 1: Trace is positive
                var s = 2 * MathF.Sqrt(T + 1);
                var w = s / 4;
                var x = (Z.Y - Y.Z) / s;
                var y = (X.Z - Z.X) / s;
                var z = (Y.X - X.Y) / s;
                return new Quaternion(x, y, z, w);
            }
            if(X.X > Y.Y && X.X > Z.Z) {
                //Case 2: R11 is the largest on the diagonal
                var s = 2 * MathF.Sqrt(1 + X.X - Y.Y - Z.Z);
                var w = (Z.Y - Y.Z) / s; 
                var x = s / 4;
                var y = (X.Z - Z.X) / s;
                var z = (Y.X - X.Y) / s;
                return new Quaternion(x, y, z, w);
            }
            if (Y.Y > Z.Z && Y.Y > X.X) {
                //Case 3: R22 is the largest on the diagonal
                var s = 2 * MathF.Sqrt(1 + Y.Y - X.X - Z.Z);
                var w = (Z.Y - Y.Z) / s;
                var x = (X.Z - Z.X) / s;
                var y = s / 4;
                var z = (Y.X - X.Y) / s;
                return new Quaternion(x, y, z, w);
            }
            if (Z.Z > X.X && Z.Z > Y.Y) {
                //Case 4: R33 is the largest on the diagonal
                var s = 2 * MathF.Sqrt(1 + Y.Y - X.X - Z.Z);
                var w = (Z.Y - Y.Z) / s;
                var x = (X.Z - Z.X) / s;
                var y = (Y.X - X.Y) / s;
                var z = s / 4;
                return new Quaternion(x, y, z, w);
            }
            //Case 5: pathological input
            throw new InvalidOperationException("Failed to calculate the quaternion");
        }
        /// <summary>
        /// Rotation-minimizing transport of <paramref name="vector"/>
        /// from (<paramref name="startPos"/>, <paramref name="startTangent"/>)
        /// to (<paramref name="endPos"/>, <paramref name="endTangent"/>)
        /// </summary>
        /// <param name="startTangent">start tangent</param>
        /// <param name="endTangent">end tangent</param>
        /// <param name="startPos">start position</param>
        /// <param name="endPos">end position</param>
        /// <param name="vector">vector to transport</param>
        /// <returns></returns>
        public static Vector3 DoubleReflection(Vector3 startTangent, Vector3 endTangent, Vector3 startPos, Vector3 endPos, Vector3 vector) {
            var v1 = endPos - startPos;
            var v2 = endTangent - ReflectVectorByNormal(startTangent, v1);
            return ReflectVectorByNormal(ReflectVectorByNormal(vector, v1), v2);
        }
        /// <summary>
        /// Calculates the counter-clockwise signed angle from <paramref name="a"/> to <paramref name="b"/> when viewed in the direction of <paramref name="normal"/>
        /// </summary>
        /// <param name="normal">view direction</param>
        /// <param name="a">starting vector</param>
        /// <param name="b">ending vector</param>
        /// <returns></returns>
        public static float SignedAngle(Vector3 normal, Vector3 a, Vector3 b) {
            var c = Vector3.Dot(a, b);
            var x = Vector3.Cross(a, b);
            var s = Vector3.Dot(normal, x);
            return MathF.Atan2(s, c);
        }

        public static float RoughSine(float x) => x * (1 - x * x * 0.16666666f);
    }
}
