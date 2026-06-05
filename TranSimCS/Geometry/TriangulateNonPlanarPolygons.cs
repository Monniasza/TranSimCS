using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Geometry {
    public struct Triangle {
        public int A;
        public int B;
        public int C;

        public Triangle(int a, int b, int c) {
            A = a;
            B = b;
            C = c;
        }
    }
    public sealed class TriangulationResult {
        public List<Vector3> Vertices { get; } = new();
        public List<int> Indices { get; } = new();
        public Transform3 ReferenceFrame;

        public int TriangleCount => Indices.Count / 3;
    }


    public class TriangulateNonPlanarPolygons {
        public static TriangulationResult Triangulate( Vector3[] polygon, float maxEdgeLength = float.PositiveInfinity) {
            if (polygon.Length < 3) throw new ArgumentException("At least 3 points are needed to triangulate");
            if (!float.IsPositive(maxEdgeLength)) throw new ArgumentException("The max edge length must be a positive real number");

            Vector3 normal = ComputeNormal(polygon);
            Vector3 center = Vector3.Zero;
            foreach (Vector3 v in polygon) center += v;
            center /= polygon.Length;

            CreateBasis(
                normal,
                out Vector3 tangent,
                out Vector3 bitangent);

            Vector3 origin = polygon[0];

            Vector2[] polygon2D = ProjectPolygon(polygon, origin, tangent, bitangent);

            List<Triangle> triangles = EarClip(polygon2D);

            TriangulationResult result = new();

            result.Vertices.AddRange(polygon);
            result.ReferenceFrame = new(bitangent, normal, tangent, center);

            List<Triangle> refined = triangles;
            if (!float.IsInfinity(maxEdgeLength)) Subdivide( result.Vertices, triangles, maxEdgeLength);

            foreach (Triangle tri in triangles) {
                result.Indices.Add(tri.A);
                result.Indices.Add(tri.B);
                result.Indices.Add(tri.C);
            }
            return result;
        }

        public static Vector3 ComputeNormal(Vector3[] vertices) {
            Vector3 normal = Vector3.Zero;

            for (int i = 0; i < vertices.Length; i++) {
                Vector3 a = vertices[i];
                Vector3 b = vertices[(i + 1) % vertices.Length];

                normal.X += (a.Y - b.Y) * (a.Z + b.Z);
                normal.Y += (a.Z - b.Z) * (a.X + b.X);
                normal.Z += (a.X - b.X) * (a.Y + b.Y);
            }

            return Vector3.Normalize(normal);
        }

        public static void CreateBasis(
            Vector3 normal,
            out Vector3 tangent,
            out Vector3 bitangent) {
            Vector3 reference =
                Math.Abs(normal.Y) < 0.99f
                    ? Vector3.UnitY
                    : Vector3.UnitX;

            tangent = Vector3.Normalize(
                Vector3.Cross(reference, normal));

            bitangent = Vector3.Cross(normal, tangent);
        }

        public static Vector2[] ProjectPolygon(Vector3[] vertices, Vector3 origin, Vector3 tangent, Vector3 bitangent) {
            Vector2[] result = new Vector2[vertices.Length];

            for (int i = 0; i < vertices.Length; i++) {
                Vector3 p = vertices[i] - origin;

                result[i] = new Vector2(
                    Vector3.Dot(p, tangent),
                    Vector3.Dot(p, bitangent));
            }

            return result;
        }

        public static List<Triangle> EarClip(Vector2[] polygon) {
            List<Triangle> triangles = new();
            List<int> indices = Enumerable.Range(0, polygon.Length).ToList();

            while (indices.Count > 3) {

                bool foundEar = false;

                for (int i = 0; i < indices.Count; i++) {

                    int prev = indices[(i - 1 + indices.Count) % indices.Count];
                    int curr = indices[i];
                    int next = indices[(i + 1) % indices.Count];

                    if (!IsConvex(
                            polygon[prev],
                            polygon[curr],
                            polygon[next]))
                        continue;

                    bool containsPoint = false;

                    for (int j = 0; j < indices.Count; j++) {

                        int p = indices[j];

                        if (p == prev || p == curr || p == next)
                            continue;

                        if (PointInTriangle(
                                polygon[p],
                                polygon[prev],
                                polygon[curr],
                                polygon[next])) {
                            containsPoint = true;
                            break;
                        }
                    }

                    if (containsPoint)
                        continue;

                    triangles.Add(new Triangle(prev, curr, next));
                    indices.RemoveAt(i);

                    foundEar = true;
                    break;
                }

                if (!foundEar)
                    throw new Exception("Polygon is invalid.");
            }

            triangles.Add(new Triangle(
                indices[0],
                indices[1],
                indices[2]));

            return triangles;
        }

        static bool IsConvex(Vector2 a, Vector2 b, Vector2 c) {
            return ((b.X - a.X) * (c.Y - a.Y)
                  - (b.Y - a.Y) * (c.X - a.X)) > 0;
        }

        public static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c) {
            float d1 = Cross(p, a, b);
            float d2 = Cross(p, b, c);
            float d3 = Cross(p, c, a);

            bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;

            return !(hasNeg && hasPos);
        }

        private static float Cross(Vector2 p1, Vector2 p2, Vector2 p3) {
            return (p1.X - p3.X) * (p2.Y - p3.Y)
                 - (p2.X - p3.X) * (p1.Y - p3.Y);
        }


        public static void Subdivide(List<Vector3> vertices, List<Triangle> triangles, float maxEdgeLength) {
            bool changed;
            do {
                changed = false;

                for (int i = 0; i < triangles.Count; i++) {

                    Triangle tri = triangles[i];

                    Vector3 a = vertices[tri.A];
                    Vector3 b = vertices[tri.B];
                    Vector3 c = vertices[tri.C];

                    float ab = Vector3.Distance(a, b);
                    float bc = Vector3.Distance(b, c);
                    float ca = Vector3.Distance(c, a);

                    float longest = Math.Max(ab,
                                    Math.Max(bc, ca));

                    if (longest <= maxEdgeLength)
                        continue;

                    int v0;
                    int v1;
                    int opposite;

                    if (longest == ab) {
                        v0 = tri.A;
                        v1 = tri.B;
                        opposite = tri.C;
                    } else if (longest == bc) {
                        v0 = tri.B;
                        v1 = tri.C;
                        opposite = tri.A;
                    } else {
                        v0 = tri.C;
                        v1 = tri.A;
                        opposite = tri.B;
                    }

                    Vector3 midpoint =
                        (vertices[v0] + vertices[v1]) * 0.5f;

                    int m = vertices.Count;
                    vertices.Add(midpoint);

                    triangles[i] =
                        new Triangle(v0, m, opposite);

                    triangles.Add(
                        new Triangle(m, v1, opposite));

                    changed = true;
                    break;
                }

            } while (changed);
        }
    }
}
