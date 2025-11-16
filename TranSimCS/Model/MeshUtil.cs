using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using NLog;
using TranSimCS.Geometry;

namespace TranSimCS.Model {
    public static class MeshUtil {
        const bool allowBVH = true;

        public static void Stats(this Mesh mesh, Logger log) {
            log.Info($"Mesh stats: verts {mesh.Vertices.Count}, indices {mesh.Indices.Count}");
        }

        public static BoundingBox BoundingBox(this Mesh mesh) {
            if(allowBVH && mesh is Mesh concrete) {
                return concrete.GetAccelerationStructure().Bounds;
            } else {
                var positions = mesh.Vertices.Select(x => x.Position);
                var min = positions.Aggregate(Vector3.Min);
                var max = positions.Aggregate(Vector3.Max);
                return new BoundingBox(min, max);
            }
        }

        
        public static object? RayIntersectMesh(Mesh mesh, Ray ray, out float intersectionDistance) {
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

        private static object? RayIntersectMeshLinear(Mesh mesh, Ray ray, out float intersectionDistance0) {
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

        public static object? RayIntersectMeshes(IEnumerable<Mesh> meshes, Ray ray, out float intersectionDistance) {
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
