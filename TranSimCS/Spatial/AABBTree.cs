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
using TranSimCS.Worlds;

namespace TranSimCS.Spatial {
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public struct AABBDiagnostic {
        public int Height;
        public float Surface;
        public int Count;

        public AABBDiagnostic(int height, float surface, int count) {
            this.Height = height;
            this.Surface = surface;
            this.Count = count;
        }

        public static AABBDiagnostic operator +(AABBDiagnostic left, AABBDiagnostic right) {
            return new(
                int.Max(left.Height, right.Height) + 1,
                left.Surface + right.Surface,
                left.Count + right.Count
            );
        }

        private string GetDebuggerDisplay() {
            return ToString();
        }

        public override string ToString() {
            return $"Height: {Height}, Surface: {Surface}, Count: {Count}";
        }
    }

    public sealed class AABBTree<T>
    where T : IObjMesh {
        private readonly Dictionary<T, AABBNode<T>> Items = [];
        private AABBNode<T>? root;

        public AABBDiagnostic[] GenerateDiagnostics() {
            if (root == null) return [new AABBDiagnostic(0, 0, 0)];
            if (root.Item != null) return [GenerateDiagnostic(root)];
            return [GenerateDiagnostic(root.Left), GenerateDiagnostic(root.Right)];
        }
        private AABBDiagnostic GenerateDiagnostic(AABBNode<T> node) {
            AABBDiagnostic diagnostic = (node.Item == null) ?
                GenerateDiagnostic(node.Left) + GenerateDiagnostic(node.Right) :
                new(1, node.Item.GetBounds().SurfaceArea(), 1);

            //Verify height
            Debug.Assert(diagnostic.Height == node.Height);
           
            return diagnostic;
        }


        public void Add(T item) {
            ArgumentNullException.ThrowIfNull(item, nameof(item));
            if(Items.ContainsKey(item)) return;

            AABBNode<T> leaf = new AABBNode<T>() {
                Item = item,
                Bounds = item.GetBounds(),
                Stale = false,
            };
            Validate(leaf);
            item.GeometryChanged += leaf.MarkStale;
            Items[item] = leaf;

            if (root == null) {
                root = leaf;
                Validate(root);
                return;
            }

            var sibling = FindBestSibling(leaf);
            InsertLeaf(leaf, sibling);
            BalanceAVL(leaf);
            RefitUpwards(leaf);
        }
        private AABBNode<T> FindBestSibling(AABBNode<T> leaf) {
            if (root == null) throw new NullReferenceException("Method called with an empty AABBTree");
            if (root.Item != null) return root;

            AABBNode<T> parent = root;
            while (true) {
                if (parent.Item != null) return parent;
                var area = leaf.SurfaceArea;
                var left = parent.Left;
                var leftMerged = BoundingBox.CreateMerged(leaf.Bounds, left.Bounds);
                var leftCost = leftMerged.SurfaceArea() - left.Bounds.SurfaceArea();
                var right = parent.Right;
                var rightMerged = BoundingBox.CreateMerged(leaf.Bounds, right.Bounds);
                var rightCost = rightMerged.SurfaceArea() - right.Bounds.SurfaceArea();
                if (leftCost < rightCost) {
                    parent = left;
                } else {
                    parent = right;
                }
            }
        }
        private void InsertLeaf(AABBNode<T> leaf, AABBNode<T> sibling) {
            Debug.Assert(leaf.Item != null); //`leaf` must be a leaf
            Debug.Assert(sibling.Item != null); //`sibling` must be a leaf

            var parent = sibling.Parent;
            if (parent != null) 
                Debug.Assert(parent.Left == sibling || parent.Right == sibling, "Sibling's parent doesn't reference sibling");
            var isSiblingOnTheLeft = parent?.Left == sibling;
            var newParent = new AABBNode<T>() {
                Left = leaf,
                Right = sibling,
                Bounds = BoundingBox.CreateMerged(leaf.Bounds, sibling.Bounds),
                Stale = false,
                Parent = parent,
                Height = 2
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
            Validate(newParent);

            //Update all ancestors
            ReheightUpwards(parent);
            Validate(parent);
        }
        private void ReheightUpwards(AABBNode<T>? parent) {
            while (parent != null) {
                Debug.Assert(parent.Item == null);
                Debug.Assert(parent.Left != null);
                Debug.Assert(parent.Right != null);
                var newHeight = int.Max(parent.Left.Height, parent.Right.Height) + 1;
                if (parent.Height == newHeight) return;
                parent.Height = newHeight;
                parent = parent.Parent;
            }
        }
        private void ReheightUpwards2(AABBNode<T>? parent) {
            while (parent != null) {
                Debug.Assert(parent.Item == null);
                Debug.Assert(parent.Left != null);
                Debug.Assert(parent.Right != null);
                var newHeight = int.Max(parent.Left.Height, parent.Right.Height) + 1;
                parent.Height = newHeight;
                Validate(parent);
                parent = parent.Parent;
            }
        }
        private void BalanceAVL(AABBNode<T> node) {
            Debug.Print("Balancing...");
            while (node != null) {
                Validate(node);
                if (node.Item != null) {
                    //This node is a leaf
                    node = node.Parent;
                    continue;
                }

                Debug.Print("Node validation succeeded");

                //Check if balancing is needed
                var bias = Balance(node);
                if(bias > -2 && bias < 2) {
                    //The node is sufficiently balanced
                    node = node.Parent;
                    continue;
                }
                if(bias > 0) {
                    //Biased left, rotate right
                    if(Balance(node.Left) < 0)
                        RotateLeft(node.Left);
                    node = RotateRight(node);
                } else {
                    //Biased right, rotate left
                    if(Balance(node.Right) > 0)
                        RotateRight(node.Right);
                    node = RotateLeft(node);
                }
                Debug.Assert(node.Left.Parent == node);
                Debug.Assert(node.Right.Parent == node);
                node = node.Parent;
            }
        }

        private int Balance(AABBNode<T> node) {
            if (node.Item != null) return 0;
            return node.Left.Height - node.Right.Height; ;
        }
        private AABBNode<T> RotateRight(AABBNode<T> O) {
            //Biased left, rotate right
            /* Before:         | B becomes the root | C becomes right of 0 | 0 becomes a left of B, finished
             * .....0.....hr>2 | ..B...0......hl0=1 | ..B...0......hl0=1   | .....B...hr=2
             * ..../.\....hl=1 | ./.\...\.....hr0=0 | ./.../.\.....hr0=1   | ..../.\..hl=2
             * ...B...A....... | D...C...A....hlB=1 | D...C...A....hlB=0   | ...D...0.....
             * ../.\.......... | .............hrB=2 | .............hrB=2   | ....../.\....
             * .D...C......... | .................. | ..................   | .....A...C...
             * .|...|......... | Modified nodes: root, B, 0, C, the predecessor */
            // Links Broken: P|0, 0/B, B\C
            // Links Added: P|B, 0/C, B\0

            var P = O.Parent;
            var B = O.Left;
            var C = B.Right;

            O.Left = C;
            C?.Parent = O;
            B.Right = O;
            O.Parent = B;
            B.Parent = P;
            if (P == null) root = B;
            else if (P.Left == O) P.Left = B;
            else if (P.Right == O) P.Right = B;
            else Debug.Fail("Neither child of the predecessor is the target node");
            if (C != null) Validate(C);
            O.Reheight(); B.Reheight();
            if (P != null) ReheightUpwards2(P);
            Validate(B); Validate(O);
            if (P != null) Validate(P);

            return B;
        }
        private AABBNode<T> RotateLeft(AABBNode<T> O) {
            //Biased right, rotate left
            /* Before:         | B becomes the root | C becomes right of 0 | 0 becomes a left of B, finished
             * .....0.....hr>2 | ..0...B......hl0=1 | ..0....B.....hl0=1   | .....B...hr=2
             * ..../.\....hl=1 | ./.../.\.....hr0=0 | ./.\....\....hr0=1   | ..../.\..hl=2
             * ...A...B....... | A...C...D....hlB=1 | A...C....D...hlB=0   | ...0...D.....
             * ....../.\...... | .............hrB=2 | .............hrB=2   | ../.\........
             * .....C...D..... | .................. | ..................   | .A...C.......
             * .....|...|..... | Modified nodes: root, B, 0, C, the predecessor */
            // Links Broken: P|0, 0\B, B/C
            // Links Added: P|B, 0\C, B/0

            var P = O.Parent;
            var B = O.Right;
            var C = B.Left;

            O.Right = C;
            C?.Parent = O;
            B.Left = O;
            O.Parent = B;
            B.Parent = P;
            if (P == null) root = B;
            else if (P.Left == O) P.Left = B;
            else if (P.Right == O) P.Right = B;
            else Debug.Fail("Neither child of the predecessor is the target node");
            if (C != null) Validate(C);
            O.Reheight(); B.Reheight();
            if (P != null) ReheightUpwards2(P);
            Validate(B); Validate(O);
            if (P != null) Validate(P);

            return B;
        }

        public bool Remove(T item) {
            ArgumentNullException.ThrowIfNull(item, nameof(item));
            if(!Items.TryGetValue(item, out var child)) return false;
            Items.Remove(item);
            item.GeometryChanged -= child.MarkStale;

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
                grandparent.Reheight();
            }

            //Destroy the parent
            parent.Destroy();
            parent = null;

            // FIX TREE
            var start = grandparent ?? sibling;

            RefitUpwards(start);
            BalanceAVL(start);

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
            RefitUpwards(node);
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
                Validate(node);
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
            Validate(node);
        }

        [Conditional("DEBUG")]
        private static void Validate(AABBNode<T> node) {
            ValidateParentChain(node);
            VectorMethods.CheckVector(node.Bounds.Min, "node.Bounds.Min");
            VectorMethods.CheckVector(node.Bounds.Min, "node.Bounds.Max");
            if (node.Item != null) {
                Debug.Assert(node.Left == null);
                Debug.Assert(node.Right == null);
                Debug.Assert(node.Height == 1);
            } else {
                Debug.Assert(node.Left != null, "Left child missing");
                Debug.Assert(node.Right != null, "Right child missing");
                Debug.Assert(node.Left.Parent == node, "Left parent incorrect");
                Debug.Assert(node.Right.Parent == node, "Right parent incorrect");
                Debug.Assert(node.Parent == null ||
                     node.Parent.Left == node ||
                     node.Parent.Right == node,
                     "Parent doesn't point back");
                int expected = int.Max(node.Left.Height, node.Right.Height) + 1;
                Debug.Assert(node.Height == expected, $"Height wrong: Actual: {node.Height}, expected: {expected}");
            }
        }

        [Conditional("DEBUG")]
        private static void ValidateParentChain(AABBNode<T>? node) {
            var slow = node;
            var fast = node;

            while (fast != null && fast.Parent != null) {
                slow = slow!.Parent;
                fast = fast.Parent.Parent;

                Debug.Assert(slow != fast, "Cycle detected in Parent chain");
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
        public int Height = 1;

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

        public void MarkStale(IObjMesh ignore) => MarkStale();
        public void MarkStale() {
            var node = this;
            while(node != null && !node.Stale) {
                node.Stale = true;
                node = node.Parent;
            }
        }
        public void Reheight() => Height = (Item == null) ? int.Max(Left.Height, Right.Height) + 1 : 1;
    }
}
