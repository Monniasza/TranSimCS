using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Menus.InGame;
using TranSimCS.Spatial;

namespace TranSimCS.SceneGraph {
    public sealed class SceneRoot {
        public SceneTree RootTree { get; }

        private readonly AABBTree<SceneProxy> tree = new();

        public SceneRoot(SceneTree root) {
            RootTree = root;

            SubscribeTree(root);
        }

        private void SubscribeTree(SceneNode node) {
            node.ChildAdded += OnAdded;
            node.ChildRemoved += OnRemoved;

            if (node is SceneTree t) {
                foreach (var c in t.Children)
                    SubscribeTree(c);
            }
        }

        private void OnAdded(SceneNode node) {
            if (node is SceneLeaf leaf)
                tree.Add(leaf.Proxy);

            if (node is SceneTree t) {
                foreach (var c in t.Children)
                    OnAdded(c);
            }

            node.ChildAdded += OnAdded;
            node.ChildRemoved += OnRemoved;
        }

        private void OnRemoved(SceneNode node) {
            if (node is SceneLeaf leaf)
                tree.Remove(leaf.Proxy);

            node.ChildAdded -= OnAdded;
            node.ChildRemoved -= OnRemoved;
        }

        public Selection Find(Ray ray, float min = 0, float max = float.PositiveInfinity) {
            tree.Find(ray, out var proxy, out var dist, out var tag);

            if (proxy == null)
                return Selection.Invalid;

            var leaf = GetLeaf(proxy);

            return new Selection {
                SceneNode = leaf,
                SelectedObj = leaf.Obj,
                Distance = dist,
                Tag = tag,
                Coordinates = ray.Position + ray.Direction * dist
            };
        }
        public IEnumerable<SceneProxy> Find(BoundingBox boundingBox) => tree.Query(boundingBox);
        public IEnumerable<SceneProxy> Find(BoundingFrustum boundingFrustum) => tree.Query(boundingFrustum);

        private SceneLeaf GetLeaf(SceneProxy proxy) {
            // safe because proxy wraps exactly one leaf
            var field = typeof(SceneProxy)
                .GetField("owner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

            return (SceneLeaf)field.GetValue(proxy)!;
        }
    }
}
