using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using NLog;
using TranSimCS.Geometry;

namespace TranSimCS.Model {
    public static class MeshUtil {
        const bool allowBVH = true;

        public static void Stats(this MeshElement mesh, Logger log) {
            log.Info($"Mesh stats: verts {mesh.Vertices0().Count}, triangles {mesh.Triangles.Length}");
        }

        public static BoundingBox BoundingBox(this MeshElement mesh) {
            return mesh.GetSpatial().Bounds;
        }

        
        public static object? RayIntersectMesh(this MeshElement mesh, Ray ray, out float intersectionDistance) {
            var bvh = mesh.GetSpatial();
            if (bvh.RayIntersect(ray, out var triId, out var hitDist)) {
                intersectionDistance = hitDist;
                return mesh.Triangles[triId].Tag;
            }
            intersectionDistance = float.MaxValue;
            return null;
        }

        public static object? RayIntersectMeshes(IEnumerable<MeshElement> meshes, Ray ray, out float intersectionDistance) {
            object? tag = null;
            float intersectionDistance0 = float.MaxValue;
            foreach (var mesh in meshes) {
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
