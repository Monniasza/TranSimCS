using System;
using System.Collections.Generic;
using System.Numerics;
using NLog;
using TranSimCS.Geometry;
using TranSimCS.SilkNet;

namespace TranSimCS.Model {
    public static class MeshUtil {
        const bool allowBVH = true;
        internal static readonly Logger DiagLog = LogManager.GetCurrentClassLogger();

        public static void Stats(this Mesh mesh, Logger log) {
            log.Info($"Mesh stats: verts {mesh.Vertices.Count}, indices {mesh.Indices.Count}");
        }

        public static AABB BoundingBox(this Mesh mesh) => mesh.GetAccelerationStructure().Bounds;

        
        public static object? RayIntersectMesh(Mesh mesh, Ray3 ray, out float intersectionDistance) {
            if (allowBVH && mesh is Mesh concrete) {
                var bvh = concrete.GetAccelerationStructure();
                if (bvh.RayIntersect(ray, out var triId, out var hitDist)) {
                    intersectionDistance = hitDist;
                    return concrete.Tags.TryGetValue(triId, out var obj) ? obj : null;
                }
                intersectionDistance = float.MaxValue;
                return null;
            } else {
                return RayIntersectMeshLinear(mesh, ray, out intersectionDistance);
            }
        }

        private static object? RayIntersectMeshLinear(Mesh mesh, Ray3 ray, out float intersectionDistance0) {
            object? tag = null;
            var minDist = float.MaxValue;
            for (int i = 0; i < mesh.Indices.Count; i += 3) {
                // Get the vertices of the triangle
                var v0 = mesh.Vertices[mesh.Indices[i]].Position;
                var v1 = mesh.Vertices[mesh.Indices[i + 1]].Position;
                var v2 = mesh.Vertices[mesh.Indices[i + 2]].Position;
                if (GeometryUtils.RayIntersectsTriangle(ray, v0, v1, v2, out float thisIntersectionDistance)) {
                    var triangleID = i / 3;
                    object? potentialTag = mesh.Tags.TryGetValue(triangleID, out var newTag) ? newTag : null;
                    if (thisIntersectionDistance < minDist) {
                        minDist = thisIntersectionDistance;
                        tag = potentialTag;
                    }
                }
            }
            intersectionDistance0 = minDist;
            return tag;
        }

        public static object? RayIntersectMeshes(IEnumerable<Mesh> meshes, Ray3 ray, out float intersectionDistance) {
            object? tag = null;
            float intersectionDistance0 = float.MaxValue;
            foreach (Mesh mesh in meshes) {
                object? tag1 = RayIntersectMesh(mesh, ray, out var intersectionDistance1);
                if (intersectionDistance1 < intersectionDistance0) {
                    intersectionDistance0 = intersectionDistance1;
                    tag = tag1;
                }
            }
            intersectionDistance = intersectionDistance0;
            return tag;
        }

        public static bool TryProjectPoint(this Mesh target, Vector3 point, Vector3 normal, out Vector3 projectedPoint, out int triangleId, out float distance, float maxDistance = float.PositiveInfinity, float minDistance = 0) {
            ArgumentNullException.ThrowIfNull(target);
            return target.GetAccelerationStructure().TryProjectPoint(point, normal, out projectedPoint, out triangleId, out distance, maxDistance, minDistance);
        }

        public static bool TryProjectVertex(this Mesh target, Vertex vertex, Vector3 normal, out Vertex projectedVertex, out int triangleId, out float distance, float maxDistance = float.PositiveInfinity, float minDistance = 0) {
            if (TryProjectPoint(target, vertex.Position, normal, out var projectedPoint, out triangleId, out distance, maxDistance, minDistance)) {
                projectedVertex = new Vertex(projectedPoint, vertex.Color, vertex.TexCoord, vertex.Material, vertex.Emissive);
                return true;
            }

            projectedVertex = default;
            return false;
        }

        /// <summary>
        /// Projects a mesh onto a target surface along <paramref name="normal"/>.
        /// Every vertex is cast along the direction; hits with signed distance in [<paramref name="minDistance"/>, <paramref name="maxDistance"/>] of the vertex are accepted.
        /// Vertices which miss the target (eg. slightly past its rim) snap to the closest point on the target within the same distance window.
        /// <paramref name="surfaceOffset"/> is re-applied along the normal after projecting, so layered geometry (markings above asphalt) keeps its separation.
        /// </summary>
        public static Mesh ProjectOnto(this Mesh projection, Mesh target, Vector3 normal, float maxDistance = float.PositiveInfinity, float minDistance = 0, float surfaceOffset = 0) {
            ArgumentNullException.ThrowIfNull(projection);
            ArgumentNullException.ThrowIfNull(target);

            var lengthSquared = normal.LengthSquared();
            if (lengthSquared <= 1e-12f)
                throw new ArgumentException("Projection normal must be non-zero.", nameof(normal));
            if (maxDistance < minDistance)
                throw new ArgumentException("Maximum distance must be greater than or equal to the minimum distance.", nameof(maxDistance));

            var direction = normal / MathF.Sqrt(lengthSquared);
            var targetBvh = target.GetAccelerationStructure();
            var projectedVertices = new List<Vertex>(projection.Vertices.Count);
            var missedVertices = 0;

            //Accept the hit closest to each vertex in either direction, with signed distance in [minDistance, maxDistance]
            //measured along the direction. The vertex may sit above or below the target surface (the target sags
            //away from the working plane), and other geometry may exist far beneath it - so both directions are
            //cast and the nearest hit wins.
            var epsilon = MeshBvh.ProjectionEpsilon;

            foreach (var vertex in projection.Vertices) {
                //Wide edge and bounds tolerance: marking vertices may sit a fraction of the polygon sagitta
                //outside the target's chord edges and bounds (eg. rim markings on an arc). Hit position comes
                //from the ray, so the tolerances only widen acceptance, never displace a hit.
                var upRay = new Ray3(vertex.Position - direction * epsilon, direction);
                var downRay = new Ray3(vertex.Position + direction * epsilon, -direction);

                bool hitUp = targetBvh.RayIntersect(upRay, MathF.Max(0, minDistance + epsilon), maxDistance + epsilon, out _, out var upDistance, edgeEpsilon: 1e-2f, boundsSlack: 1e-2f);
                bool hitDown = targetBvh.RayIntersect(downRay, MathF.Max(0, -maxDistance + epsilon), -minDistance + epsilon, out _, out var downDistance, edgeEpsilon: 1e-2f, boundsSlack: 1e-2f);
                float upSigned = upDistance - epsilon;
                float downSigned = -(downDistance - epsilon);

                Vector3 hitPoint;
                if (hitUp && (!hitDown || MathF.Abs(upSigned) <= MathF.Abs(downSigned))) {
                    hitPoint = upRay.GetPoint(upDistance) + direction * surfaceOffset;
                } else if (hitDown) {
                    hitPoint = downRay.GetPoint(downDistance) + direction * surfaceOffset;
                } else {
                    missedVertices++;
                    var fallback = ClosestPointOnMesh(target, vertex.Position);
                    var delta = Vector3.Dot(fallback - vertex.Position, direction);
                    if (delta >= minDistance && delta <= maxDistance)
                        fallback += direction * surfaceOffset;
                    else
                        fallback = vertex.Position;
                    hitPoint = fallback;
                }
                projectedVertices.Add(new Vertex(
                    hitPoint,
                    vertex.Color,
                    vertex.TexCoord,
                    vertex.Material,
                    vertex.Emissive));
            }

            return new Mesh(null, projectedVertices, projection.Indices, projection.Tags);
        }

        /// <summary>
        /// Projects every render bin of a multimesh onto a target surface along <paramref name="normal"/>.
        /// Vertices which miss the target snap to the closest point on the target.
        /// </summary>
        public static MultiMesh ProjectOnto(this MultiMesh projection, Mesh target, Vector3 normal, float maxDistance = float.PositiveInfinity, float minDistance = 0, float surfaceOffset = 0) {
            ArgumentNullException.ThrowIfNull(projection);
            ArgumentNullException.ThrowIfNull(target);

            var result = new MultiMesh();
            foreach (var bin in projection.RenderBins)
                result.GetOrCreateRenderBin(bin.Key, null).DrawModel(bin.Value.ProjectOnto(target, normal, maxDistance, minDistance, surfaceOffset));
            return result;
        }

        /// <summary>Closest point on the surface of a triangle to <paramref name="point"/> (Ericson, Real-Time Collision Detection).</summary>
        public static Vector3 ClosestPointOnTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c) {
            var ab = b - a;
            var ac = c - a;
            var ap = point - a;
            var d1 = Vector3.Dot(ab, ap);
            var d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) return a;

            var bp = point - b;
            var d3 = Vector3.Dot(ab, bp);
            var d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) return b;

            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0) {
                var v = d1 / (d1 - d3);
                return a + ab * v;
            }

            var cp = point - c;
            var d5 = Vector3.Dot(ab, cp);
            var d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) return c;

            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0) {
                var w = d2 / (d2 - d6);
                return a + ac * w;
            }

            var va = d3 * d6 - d5 * d4;
            if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0) {
                var w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + (c - b) * w;
            }

            var denom = 1 / (va + vb + vc);
            return a + ab * (vb * denom) + ac * (vc * denom);
        }

        private static Vector3 ClosestPointOnMesh(Mesh mesh, Vector3 point) {
            var verts = mesh.Vertices;
            var indices = mesh.Indices;
            var best = point;
            var bestDistance = float.MaxValue;
            for (int i = 0; i <= indices.Count - 3; i += 3) {
                var candidate = ClosestPointOnTriangle(point,
                    verts[indices[i]].Position,
                    verts[indices[i + 1]].Position,
                    verts[indices[i + 2]].Position);
                var d = Vector3.DistanceSquared(point, candidate);
                if (d < bestDistance) {
                    bestDistance = d;
                    best = candidate;
                }
            }
            return best;
        }

        public static void ReverseWinding(this Mesh mesh) {
            for (int i = 0; i < mesh.Indices.Count; i += 3) {
                DataUtil.Swap(mesh.Indices, i, i + 1);
            }
        }
        public static void ReverseWinding(this MultiMesh multiMesh) {
            foreach (var mesh in multiMesh.RenderBins.Values) mesh.ReverseWinding();
        }

        public static T[] TriangleFan<T>(IList<T> polygon) {
            int tricount = polygon.Count - 2;
            T[] values = new T[tricount * 3];
            for (int i = 0; i < tricount; i++) {
                var idx0 = 0;
                var idx1 = i;
                var idx2 = i+1;
                values[i*3] = polygon[idx0];
                values[i*3+1] = polygon[idx1];
                values[i*3+2] = polygon[idx2];
            }
            return values;
        }
    }
}
