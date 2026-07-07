using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.SceneGraph;

namespace TranSimCS.Spatial {
    public sealed class AABBTree<T>
    where T : IMeshSource {
        private readonly Dictionary<T, AABBNode<T>> Items = [];
        private AABBNode<T>? root;

        public void Add(T item) {
            ArgumentNullException.ThrowIfNull(item, nameof(item));
            if(Items.ContainsKey(item)) return;

            AABBNode<T> leaf = new AABBNode<T>() {
                Item = item,
                Bounds = item.GetBounds(),
                Stale = false,
            };
            item.OnMeshInvalidated += leaf.MarkStale;
            Items[item] = leaf;

            if (root == null) {
                root = leaf;
                Validate(root);
                return;
            }

            var sibling = FindBestSibling(leaf);
            InsertLeaf(leaf, sibling);
            Balance(leaf);
            RefitUpwards(leaf);
        }
        private AABBNode<T> FindBestSibling(AABBNode<T> leaf) {
            if (root == null) throw new NullReferenceException("Method called with an empty AABBTree");
            if (root.Item != null) return root;

            AABBNode<T> parent = root;
            while (true) {
                if (parent.Item != null) return parent;
                if (parent.Left != null && parent.Right != null) return parent;
                var area = leaf.SurfaceArea;
                var left = parent.Left;
                var leftMerged = BoundingBox.CreateMerged(leaf.Bounds, left.Bounds);
                var leftCost = leftMerged.SurfaceArea() - area;
                var right = parent.Right;
                var rightMerged = BoundingBox.CreateMerged(leaf.Bounds, right.Bounds);
                var rightCost = rightMerged.SurfaceArea() - area;
                if (leftCost < rightCost) {
                    parent = left;
                } else {
                    parent = right;
                }
            }
        }
        private void InsertLeaf(AABBNode<T> leaf, AABBNode<T> sibling) {
            var parent = sibling.Parent;
            var isSiblingOnTheLeft = parent?.Left == sibling;
            var newParent = new AABBNode<T>() {
                Left = leaf,
                Right = sibling,
                Bounds = BoundingBox.CreateMerged(leaf.Bounds, sibling.Bounds),
                Stale = false,
                Parent = parent,
            };
            leaf.Parent = newParent;
            sibling.Parent = newParent;

            if (parent == null) {
                root = newParent;
                return;
            }
            if (isSiblingOnTheLeft) {
                parent.Left = newParent;
            } else {
                parent.Right = newParent;
            }
            newParent.Parent = parent;
        }
        
        private void Balance(AABBNode<T> node) {
            while (node != null) {
                if (node.Item != null) {
                    node = node.Parent;
                    continue;
                }

                var left = node.Left;
                var right = node.Right;

                if (left == null || right == null) {
                    node = node.Parent;
                    continue;
                }

                float bestCost = node.Bounds.SurfaceArea();

                AABBNode<T>? bestA = null;
                AABBNode<T>? bestB = null;

                // -------------------------
                // Try LEFT rotation (bring right child up)
                // -------------------------
                if (right.Item == null) {
                    var rl = right.Left;
                    var rr = right.Right;

                    float cost = BoundingBox.CreateMerged(
                        left.Bounds,
                        rl.Bounds).SurfaceArea();

                    cost += BoundingBox.CreateMerged(
                        rr.Bounds,
                        node.Bounds).SurfaceArea();

                    if (cost < bestCost) {
                        bestCost = cost;
                        bestA = right;
                        bestB = left;
                    }
                }

                // -------------------------
                // Try RIGHT rotation (bring left child up)
                // -------------------------
                if (left.Item == null) {
                    var ll = left.Left;
                    var lr = left.Right;

                    float cost = BoundingBox.CreateMerged(
                        ll.Bounds,
                        node.Bounds).SurfaceArea();

                    cost += BoundingBox.CreateMerged(
                        lr.Bounds,
                        right.Bounds).SurfaceArea();

                    if (cost < bestCost) {
                        bestCost = cost;
                        bestA = left;
                        bestB = right;
                    }
                }

                // -------------------------
                // Apply best rotation
                // -------------------------
                if (bestA != null) {
                    ApplyRotation(node, bestA, bestB);
                }

                node = node.Parent;
            }
        }
        private void ApplyRotation(AABBNode<T> parent, AABBNode<T> child, AABBNode<T> sibling) {
            var grandParent = parent.Parent;

            // detach
            if (grandParent != null) {
                if (grandParent.Left == parent)
                    grandParent.Left = child;
                else
                    grandParent.Right = child;
            } else {
                root = child;
            }

            child.Parent = grandParent;

            // rotate structure
            if (child == parent.Left) {
                // RIGHT rotation
                var move = child.Right;

                child.Right = parent;
                parent.Parent = child;

                parent.Left = move;
                if (move != null) move.Parent = parent;
            } else {
                // LEFT rotation
                var move = child.Left;

                child.Left = parent;
                parent.Parent = child;

                parent.Right = move;
                if (move != null) move.Parent = parent;
            }

            // fix bounds
            parent.Bounds = MergeBounds(parent);
            child.Bounds = MergeBounds(child);
        }

        private BoundingBox MergeBounds(AABBNode<T> n) {
            if (n.Item != null)
                return n.Item.GetBounds();

            return BoundingBox.CreateMerged(n.Left.Bounds, n.Right.Bounds);
        }

        public bool Remove(T item) {
            ArgumentNullException.ThrowIfNull(item, nameof(item));
            if(!Items.TryGetValue(item, out var child)) return false;
            Items.Remove(item);
            item.OnMeshInvalidated -= child.MarkStale;

            //Move the sibling into place of the parent
            if(child == root || child.Parent == null) {
                //Deleting the last element
                root = null;
                return true;
            }

            var isChildLeftOfParent = child.Parent.Left == child;
            var parent = child.Parent;
            var sibling = isChildLeftOfParent ? parent.Right : parent.Left;
            var grandparent = child.Parent.Parent;

            //Fully invalidate the child
            child.Destroy();
            child = null;
            
            sibling.Parent = grandparent;
            if (grandparent == null) {
                //Deleting a second-level node
                root = sibling;
            } else {
                var isParentLeftOfGrand = grandparent.Left == parent;
                if (isParentLeftOfGrand) {
                    grandparent.Left = sibling;
                } else {
                    grandparent.Right = sibling;
                }
            }

            //Destroy the parent
            parent.Destroy();
            parent = null;

            // FIX TREE
            var start = grandparent ?? sibling;

            RefitUpwards(start);
            Balance(start);

            return true;
        }
        public void Clear() {
            Items.Clear();
            root = null;
        }

        public bool Find(Ray ray, out T element, out float distance, out object? tag) {
            Reject(out element, out distance, out tag);
            if (root == null) return false;

            PriorityQueue<AABBNode<T>, float> queue = new();
            
            void ComputeIntersectionAndAddElementToQueue(AABBNode<T>? node, float upperBound = float.PositiveInfinity) {
                if (node == null) return;
                Refit(node);
                var rayIntersect = node.Bounds.Intersects(ray);
                if (rayIntersect == null || rayIntersect < 0 || rayIntersect > upperBound) return;
                queue.Enqueue(node, rayIntersect.Value);
            }

            bool intersectionFound = false;
            ComputeIntersectionAndAddElementToQueue(root);
            while (queue.TryDequeue(out var node, out var priority) && priority <= distance) {
                if(node.Item != null) {
                    var exactIntersection = node.Item.ComputeIntersection(ray, out var newdistance, out var newtag);
                    if (!exactIntersection || newdistance > distance) continue;
                    intersectionFound = true;
                    element = node.Item;
                    distance = newdistance;
                    tag = newtag;
                } else {
                    ComputeIntersectionAndAddElementToQueue(node.Left, distance);
                    ComputeIntersectionAndAddElementToQueue(node.Right, distance);
                }
            }
            return intersectionFound;
        }

        private bool Reject(out T node, out float distance, out object? tag) {
            node = default(T);
            distance = float.PositiveInfinity;
            tag = null;
            return false;
        }

        public IEnumerable<T> Query(BoundingBox box) {
            Queue<AABBNode<T>?> queue = new();
            queue.Enqueue(root);
            Refit(root);
            while(queue.TryDequeue(out var node)) {
                if(node == null) continue;
                var nodeBox = node.Bounds;
                if (nodeBox.Intersects(box)) {
                    if (node.Item != null) yield return node.Item;
                    queue.Enqueue(node.Left);
                    queue.Enqueue(node.Right);
                }
            }
        }

        public IEnumerable<T> Query(BoundingFrustum frustum) {
            Queue<AABBNode<T>?> queue = new();
            queue.Enqueue(root);
            Refit(root);
            while (queue.TryDequeue(out var node)) {
                if (node == null) continue;
                var nodeBox = node.Bounds;
                var contains = frustum.Contains(nodeBox);
                if (contains != ContainmentType.Disjoint) {
                    if (node.Item != null) yield return node.Item;
                    queue.Enqueue(node.Left);
                    queue.Enqueue(node.Right);
                }
            }
        }

        private void Refit(AABBNode<T>? node) {
            if (node == null || !node.Stale) return;
            RefitDown(node);
            RefitUpwards(node.Parent);
        }
        private void RefitUpwards(AABBNode<T> node) {
            Debug.Assert(node != null);
            while (node != null) {
                if (node.Item == null) {
                    Debug.Assert(node.Left != null);
                    Debug.Assert(node.Right != null);
                    node.Bounds = BoundingBox.CreateMerged(
                        node.Left.Bounds, node.Right.Bounds
                    );
                } else {
                    node.Bounds = node.Item.GetBounds();
                }
                node = node.Parent;
            }
        }
        private void RefitDown(AABBNode<T> node) {
            if (node == null || !node.Stale) return;
            Validate(node);
            if (node.Item != null) {
                node.Bounds = node.Item.GetBounds();
                node.Stale = false;
            } else {
                RefitDown(node.Left);
                RefitDown(node.Right);
                node.Bounds = BoundingBox.CreateMerged(node.Left.Bounds, node.Right.Bounds);
                var isLeftStale = node.Left != null && node.Left.Stale;
                var isRightStale = node.Right != null && node.Right.Stale;
                if (!isLeftStale && !isRightStale) node.Stale = false;
            }
        }

        [Conditional("DEBUG")]
        private static void Validate(AABBNode<T> node) {
            if (node.Item != null) {
                Debug.Assert(node.Left == null);
                Debug.Assert(node.Right == null);
            } else {
                Debug.Assert(node.Left != null);
                Debug.Assert(node.Right != null);
            }
        }
    }
    public sealed class AABBNode<T> {
        public AABBNode(){}
        public AABBNode<T>? Parent;
        public AABBNode<T>? Left;
        public AABBNode<T>? Right;
        public BoundingBox Bounds;
        public bool Stale;
        public float SurfaceArea => Bounds.SurfaceArea();
        public T? Item;
        public void Destroy() {
            Left = null;
            Right = null;
            Parent = null;
            Bounds = default;
            Item = default;
            Stale = true;
        }
        public void MarkStale() {
            var node = this;
            while(node != null && !node.Stale) {
                node.Stale = true;
                node = node.Parent;
            }
        }
    }
}
