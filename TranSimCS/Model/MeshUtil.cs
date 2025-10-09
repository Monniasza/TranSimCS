using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Model {
    public static class MeshUtil {
        const bool allowBVH = true;
        public static object? RayIntersectMesh(IRenderBin mesh, Ray ray, out float intersectionDistance) {
            object? tag = null;
            float closest = float.MaxValue;

            if (allowBVH && mesh is Mesh concrete) {
                var bvh = concrete.GetAccelerationStructure();
                if (bvh.RayIntersect(ray, out var triId, out var hitDist)) {
                    closest = hitDist;
                    tag = concrete.Tags.TryGetValue(triId, out var obj) ? obj : null;
                }
            } else {
                tag = RayIntersectMeshLinear(mesh, ray, ref closest);
            }
            intersectionDistance = closest;
            return tag;
        }

        private static object? RayIntersectMeshLinear(IRenderBin mesh, Ray ray, ref float intersectionDistance0) {
            object? tag = null;
            for (int i = 0; i < mesh.Indices.Count; i += 3) {
                // Get the vertices of the triangle
                var v0 = mesh.Vertices[mesh.Indices[i]].Position;
                var v1 = mesh.Vertices[mesh.Indices[i + 1]].Position;
                var v2 = mesh.Vertices[mesh.Indices[i + 2]].Position;
                if (GeometryUtils.RayIntersectsTriangle(ray, v0, v1, v2, out float thisIntersectionDistance)) {
                    var triangleID = i / 3;
                    object? potentialTag = mesh.Tags.TryGetValue(triangleID, out var newTag) ? newTag : null;
                    if (thisIntersectionDistance < intersectionDistance0) {
                        intersectionDistance0 = thisIntersectionDistance;
                        tag = potentialTag;
                    }
                }
            }
            return tag;
        }

        public static object? RayIntersectMeshes(IEnumerable<IRenderBin> meshes, Ray ray, out float intersectionDistance) {
            object? tag = null;
            float intersectionDistance0 = float.MaxValue;
            foreach (IRenderBin mesh in meshes) {
                object? tag1 = RayIntersectMesh(mesh, ray, out var intersectionDistance1);
                if (intersectionDistance1 < intersectionDistance0) {
                    intersectionDistance0 = intersectionDistance1;
                    tag = tag1;
                }
            }
            intersectionDistance = intersectionDistance0;
            return tag;
        }
    }
}
