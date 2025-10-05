using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Model {
    internal sealed class MeshBvh {
        private const int LeafSize = 4;
        private readonly Mesh mesh;
        private readonly Node[] nodes;
        private readonly Triangle[] triangles;

        private struct Node {
            public BoundingBox Bounds;
            public int Left;
            public int Right;
            public int Start;
            public int Count;
            public bool IsLeaf => Count > 0;
        }

        private struct Triangle {
            public BoundingBox Bounds;
            public Vector3 Centroid;
            public int BaseIndex;
            public int Id;
        }

        private MeshBvh(Mesh mesh, Node[] nodes, Triangle[] triangles) {
            this.mesh = mesh;
            this.nodes = nodes;
            this.triangles = triangles;
        }

        public BoundingBox Bounds => nodes.Length == 0 ? default : nodes[0].Bounds;

        public static MeshBvh Build(Mesh mesh) {
            var indices = mesh.Indices;
            var vertices = mesh.Vertices;
            var tris = new List<Triangle>(indices.Count / 3);
            for (int i = 0, id = 0; i <= indices.Count - 3; i += 3, id++) {
                var p0 = vertices[indices[i]].Position;
                var p1 = vertices[indices[i + 1]].Position;
                var p2 = vertices[indices[i + 2]].Position;
                var min = Vector3.Min(Vector3.Min(p0, p1), p2);
                var max = Vector3.Max(Vector3.Max(p0, p1), p2);
                tris.Add(new Triangle {
                    Bounds = new BoundingBox(min, max),
                    Centroid = (p0 + p1 + p2) / 3f,
                    BaseIndex = i,
                    Id = id
                });
            }
            var nodes = new List<Node>(tris.Count * 2);
            if (tris.Count > 0)
                BuildRecursive(tris, 0, tris.Count, nodes);
            return new MeshBvh(mesh, nodes.ToArray(), tris.ToArray());
        }

        private static int BuildRecursive(List<Triangle> tris, int start, int count, List<Node> nodes) {
            var bounds = ComputeBounds(tris, start, count);
            var nodeIndex = nodes.Count;
            nodes.Add(new Node { Bounds = bounds, Left = -1, Right = -1, Start = start, Count = count });
            if (count <= LeafSize) return nodeIndex;
            var centroidBounds = ComputeCentroidBounds(tris, start, count);
            var size = centroidBounds.Max - centroidBounds.Min;
            float maxExtent = Math.Max(size.X, Math.Max(size.Y, size.Z));
            if (maxExtent <= 1e-5f) return nodeIndex;
            int axis = size.X >= size.Y && size.X >= size.Z ? 0 : size.Y >= size.Z ? 1 : 2;
            int mid = start + count / 2;
            tris.Sort(start, count, Comparer<Triangle>.Create((a, b) => axis switch {
                0 => a.Centroid.X.CompareTo(b.Centroid.X),
                1 => a.Centroid.Y.CompareTo(b.Centroid.Y),
                _ => a.Centroid.Z.CompareTo(b.Centroid.Z)
            }));
            int left = BuildRecursive(tris, start, mid - start, nodes);
            int right = BuildRecursive(tris, mid, start + count - mid, nodes);
            nodes[nodeIndex] = new Node { Bounds = bounds, Left = left, Right = right, Start = start, Count = 0 };
            return nodeIndex;
        }

        private static BoundingBox ComputeBounds(List<Triangle> tris, int start, int count) {
            var bounds = tris[start].Bounds;
            for (int i = 1; i < count; i++)
                bounds = BoundingBox.CreateMerged(bounds, tris[start + i].Bounds);
            return bounds;
        }

        private static BoundingBox ComputeCentroidBounds(List<Triangle> tris, int start, int count) {
            Vector3 min = tris[start].Centroid, max = min;
            for (int i = 1; i < count; i++) {
                var c = tris[start + i].Centroid;
                min = Vector3.Min(min, c);
                max = Vector3.Max(max, c);
            }
            return new BoundingBox(min, max);
        }

        public bool RayIntersect(Ray ray, out int triangleId, out float distance) {
            triangleId = -1;
            distance = float.MaxValue;
            if (nodes.Length == 0) return false;
            Span<int> stack = nodes.Length <= 128 ? stackalloc int[128] : new int[nodes.Length];
            int stackSize = 0;
            stack[stackSize++] = 0;
            var verts = mesh.Vertices;
            var indices = mesh.Indices;
            while (stackSize > 0) {
                var node = nodes[stack[--stackSize]];
                var hit = ray.Intersects(node.Bounds);
                if (!hit.HasValue || hit.Value > distance) continue;
                if (node.IsLeaf) {
                    for (int i = 0; i < node.Count; i++) {
                        var tri = triangles[node.Start + i];
                        if (GeometryUtils.RayIntersectsTriangle(ray,
                            verts[indices[tri.BaseIndex]].Position,
                            verts[indices[tri.BaseIndex + 1]].Position,
                            verts[indices[tri.BaseIndex + 2]].Position,
                            out float triDist, 1e-6f, distance) && triDist < distance) {
                            distance = triDist;
                            triangleId = tri.Id;
                        }
                    }
                } else {
                    if (node.Left >= 0) stack[stackSize++] = node.Left;
                    if (node.Right >= 0) stack[stackSize++] = node.Right;
                }
            }
            return triangleId >= 0;
        }
    }
}
