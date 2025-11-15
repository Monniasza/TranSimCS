using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Model {
    public abstract class MeshBvh {
        protected struct Node {
            public BoundingBox Bounds;
            public int Left;
            public int Right;
            public int Start;
            public int Length;
            public bool IsLeaf => Length > 0;
        }

        protected struct Triangle {
            public BoundingBox Bounds;
            public Vector3 Centroid;
            public int BaseIndex;
            public int Id;
        }

        public abstract bool RayIntersect(Ray ray, out int triangleId, out float distance);

        protected readonly Node[] nodes;
        protected readonly Triangle[] triangles;
        public BoundingBox Bounds => nodes.Length == 0 ? default : nodes[0].Bounds;

        protected MeshBvh(Node[] nodes, Triangle[] triangles) {
            this.nodes = nodes;
            this.triangles = triangles;
        }
    }
    public sealed class MeshBvh<TMaterial, TVertex>: MeshBvh {
        private const int LeafSize = 4;
        private readonly MeshElement<TMaterial, TVertex> mesh;
        
        private MeshBvh(MeshElement<TMaterial, TVertex> mesh, Node[] nodes, Triangle[] triangles):
            base(nodes, triangles) {
            this.mesh = mesh; 
        }        

        public static MeshBvh<TMaterial, TVertex> Build(MeshElement<TMaterial, TVertex> mesh) {
            var meshtris = mesh.Triangles;
            var vertices = mesh.Vertices;
            var tris = new Triangle[meshtris.Length];
            for (int i = 0;  i <= tris.Length; i++) {
                var meshtri = meshtris[i];
                var meshProcessor = mesh.GetVertexProcessorStrict();
                var v0 = vertices[meshtri.A];
                var v1 = vertices[meshtri.B];
                var v2 = vertices[meshtri.C];
                var p0 = meshProcessor.GetVertexCoords(v0);
                var p1 = meshProcessor.GetVertexCoords(v1);
                var p2 = meshProcessor.GetVertexCoords(v2);
                var min = Vector3.Min(Vector3.Min(p0, p1), p2);
                var max = Vector3.Max(Vector3.Max(p0, p1), p2);
                tris[i] = new Triangle {
                    Bounds = new BoundingBox(min, max),
                    Centroid = (p0 + p1 + p2) / 3f,
                    BaseIndex = i,
                    Id = i
                };
            }
            var nodes = new List<Node>(tris.Length * 2);
            if (tris.Length > 0)
                BuildRecursive(tris, 0, tris.Length, nodes);
            return new MeshBvh<TMaterial, TVertex>(mesh, nodes.ToArray(), tris.ToArray());
        }

        private static int BuildRecursive(Triangle[] tris, int start, int count, List<Node> nodes) {
            var bounds = ComputeBounds(tris, start, count);
            var nodeIndex = nodes.Count;
            nodes.Add(new Node { Bounds = bounds, Left = -1, Right = -1, Start = start, Length = count });
            if (count <= LeafSize) return nodeIndex;
            var centroidBounds = ComputeCentroidBounds(tris, start, count);
            var size = centroidBounds.Max - centroidBounds.Min;
            float maxExtent = Math.Max(size.X, Math.Max(size.Y, size.Z));
            if (maxExtent <= 1e-5f) return nodeIndex;
            int axis = size.X >= size.Y && size.X >= size.Z ? 0 : size.Y >= size.Z ? 1 : 2;
            int mid = start + count / 2;
            Array.Sort<Triangle>(tris, start, count, Comparer<Triangle>.Create((a, b) => axis switch {
                0 => a.Centroid.X.CompareTo(b.Centroid.X),
                1 => a.Centroid.Y.CompareTo(b.Centroid.Y),
                _ => a.Centroid.Z.CompareTo(b.Centroid.Z)
            }));
            int left = BuildRecursive(tris, start, mid - start, nodes);
            int right = BuildRecursive(tris, mid, start + count - mid, nodes);
            nodes[nodeIndex] = new Node { Bounds = bounds, Left = left, Right = right, Start = start, Length = 0 };
            return nodeIndex;
        }

        private static BoundingBox ComputeBounds(Triangle[] tris, int start, int count) {
            var bounds = tris[start].Bounds;
            for (int i = 1; i < count; i++)
                bounds = BoundingBox.CreateMerged(bounds, tris[start + i].Bounds);
            return bounds;
        }

        private static BoundingBox ComputeCentroidBounds(Triangle[] tris, int start, int count) {
            Vector3 min = tris[start].Centroid, max = min;
            for (int i = 1; i < count; i++) {
                var c = tris[start + i].Centroid;
                min = Vector3.Min(min, c);
                max = Vector3.Max(max, c);
            }
            return new BoundingBox(min, max);
        }

        public override bool RayIntersect(Ray ray, out int triangleId, out float distance) {
            triangleId = -1;
            distance = float.MaxValue;
            if (nodes.Length == 0) return false;
            Span<int> stack = nodes.Length <= 128 ? stackalloc int[128] : new int[nodes.Length];
            int stackSize = 0;
            stack[stackSize++] = 0;
            var verts = mesh.Vertices;
            var tris = mesh.Triangles;
            var processor = mesh.GetVertexProcessorStrict();
            while (stackSize > 0) {
                var node = nodes[stack[--stackSize]];
                var hit = ray.Intersects(node.Bounds);
                if (!hit.HasValue || hit.Value > distance) continue;
                if (node.IsLeaf) {
                    for (int i = 0; i < node.Length; i++) {
                        var tri = triangles[node.Start + i];
                        var p0 = processor.GetVertexCoords(verts[tris[tri.BaseIndex].A]);
                        var p1 = processor.GetVertexCoords(verts[tris[tri.BaseIndex].B]);
                        var p2 = processor.GetVertexCoords(verts[tris[tri.BaseIndex].C]);
                        if (GeometryUtils.RayIntersectsTriangle(ray,
                            p0, p1, p2,
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
