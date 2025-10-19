using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Spatial {
    public interface IBVHElement {
        public BoundingBox GetBounds();
        public Vector3 Centroid() {
            var bounds = GetBounds();
            return (bounds.Max + bounds.Min) / 2;
        }
            
        public bool ComputeIntersection(Ray ray, out float distance, out object? tag);
    }
    public sealed class ElementBVH<T> where T : IBVHElement {
        private const int LeafSize = 4;
        private readonly Node[] nodes;
        private readonly T[] objects;

        private struct Node {
            public BoundingBox Bounds;
            public int Left;
            public int Right;
            public int Start;
            public int Count;
            public bool IsLeaf => Count > 0;
            public Vector3 Centroid => (Bounds.Max + Bounds.Min) / 2;
        }

        public ElementBVH(List<T> objects) {
            this.objects = objects.ToArray();
            var nodesList = new List<Node>();
            if (objects.Count > 0)
                BuildRecursive(objects, 0, objects.Count, nodesList);
            nodes = nodesList.ToArray();
        }

        public BoundingBox Bounds => nodes.Length == 0 ? default : nodes[0].Bounds;

        private int BuildRecursive(List<T> objects, int start, int count, List<Node> nodes) {
            var bounds = ComputeBounds(start, count);
            var nodeIndex = nodes.Count;
            nodes.Add(new Node { Bounds = bounds, Left = -1, Right = -1, Start = start, Count = count });
            if (count <= LeafSize) return nodeIndex;
            var centroidBounds = ComputeCentroidBounds(start, count);
            var size = centroidBounds.Max - centroidBounds.Min;
            float maxExtent = Math.Max(size.X, Math.Max(size.Y, size.Z));
            if (maxExtent <= 1e-5f) return nodeIndex;
            int axis = size.X >= size.Y && size.X >= size.Z ? 0 : size.Y >= size.Z ? 1 : 2;
            int mid = start + count / 2;
            Sort(objects, start, count, axis);
            int left = BuildRecursive(objects, start, mid - start, nodes);
            int right = BuildRecursive(objects, mid, start + count - mid, nodes);
            nodes[nodeIndex] = new Node { Bounds = bounds, Left = left, Right = right, Start = start, Count = 0 };
            return nodeIndex;
        }

        private BoundingBox ComputeBounds(int start, int count) {
            var bounds = objects[start].GetBounds();
            for (int i = 1; i < count; i++)
                bounds = BoundingBox.CreateMerged(bounds, objects[start + i].GetBounds());
            return bounds;
        }

        private BoundingBox ComputeCentroidBounds(int start, int count) {
            Vector3 min = objects[start].GetBounds().Min, max = min;
            for (int i = 1; i < count; i++) {
                var c = objects[start + i].GetBounds().Min;
                min = Vector3.Min(min, c);
                max = Vector3.Max(max, c);
            }
            return new BoundingBox(min, max);
        }

        private void Sort(List<T> nodes, int start, int count, int axis) {
            // Implement sorting based on the selected axis.
            nodes.Sort(start, count, Comparer<T>.Create((a, b) => axis switch {
                 0 => a.Centroid().X.CompareTo(b.Centroid().X),
                 1 => a.Centroid().Y.CompareTo(b.Centroid().Y),
                 _ => a.Centroid().Z.CompareTo(b.Centroid().Z)
             }));
        }

        public bool RayIntersect(Ray ray, out float distance, out object? tag) {
            distance = float.MaxValue;
            tag = null;

            if (nodes.Length == 0) return false;
            Span<int> stack = nodes.Length <= 128 ? stackalloc int[128] : new int[nodes.Length];
            int stackSize = 0;
            stack[stackSize++] = 0;

            while (stackSize > 0) {
                var nodeIndex = stack[--stackSize];
                var node = nodes[nodeIndex];
                if ((node.Bounds.Intersects(ray) ?? 0) <= 0) continue;
                if (node.IsLeaf) {
                    for (int i = 0; i < node.Count; i++) {
                        int objIndex = node.Start + i;
                        if (objects[objIndex].ComputeIntersection(ray, out float dist, out var candidateTag) && dist < distance) {
                            distance = dist;
                            tag = candidateTag;
                        }
                    }
                } else {
                    if (node.Left >= 0) stack[stackSize++] = node.Left;
                    if (node.Right >= 0) stack[stackSize++] = node.Right;
                }
            }
            var result = distance != float.MaxValue;
            return result;
        }
    }
}
