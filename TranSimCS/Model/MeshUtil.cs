using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TranSimCS.Model {
    public static class MeshUtil {
        public static object? RayIntersectMesh(IRenderBin mesh, Ray ray, out float intersectionDistance) {
            object? tag = null;
            float closest = float.MaxValue;
            if (mesh is Mesh concrete) {
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
                if (Geometry.RayIntersectsTriangle(ray, v0, v1, v2, out float thisIntersectionDistance, 1e-6f, intersectionDistance0)) {
                    object? potentialTag = mesh.Tags.ContainsKey(i / 3) ? mesh.Tags[i / 3] : null;
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
