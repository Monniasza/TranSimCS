using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Spatial {
    /// <summary>
    /// R-tree. Ported from https://github.com/marchello2000/RTree/blob/master/src/RTree/RTree.cs#L175
    /// </summary>
    /// <typeparam name="T">type of data values</typeparam>
    public partial class RTreeBroken<T> {
        private static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;
        private static readonly Comparer<Node> xComparer = RTreeCalcs.CreateNodeComparer<T>(a => a.X);
        private static readonly Comparer<Node> yComparer = RTreeCalcs.CreateNodeComparer<T>(a => a.Y);
        private static readonly Comparer<Node> zComparer = RTreeCalcs.CreateNodeComparer<T>(a => a.Z);
        private Node root;

        //Per-bucket
        private readonly int maxEntries;
        private readonly int minEntries;

        public RTreeBroken(int maxEntries = 9) {
            this.maxEntries = maxEntries;
            this.minEntries = (int)Math.Max(2, Math.Ceiling(this.maxEntries * 0.4));
            Clear();
        }

        public BoundingBox Bounds() {
            return root.BoundingBox;
        }
        public void Load(List<Node> nodes) {
            if(nodes.Count < minEntries) {
                foreach (var n in nodes) Insert(n);
                return;
            }

            //recursively build the tree with the given data from scratch using OMT algorithm
            var node = BuildOneLevel(nodes, 0, 0);
            if(root.Children.Count == 0) {
                //save as is if tree is empty
                root = node;
            }else if(root.Height == node.Height) {
                //split root if trees have the same height
                SplitRoot(root, node);
            } else {
                if(root.Height < node.Height) 
                    // swap trees if inserted one is bigger
                    (root, node) = (node, root);
                // insert the small tree into the large tree at appropriate level
                Insert(node, root.Height - node.Height - 1);
            }
        }

        private Node BuildOneLevel(List<Node> items, int level, int height) {
            Node node;
            var N = items.Count;
            var M = maxEntries;
            if(N <= M) {
                node = new Node { IsLeaf = true, Height = 1 };
                node.Children.AddRange(items);
            } else {
                if(level == 0) {
                    // target height of the bulk-loaded tree
                    height = (int)Math.Ceiling(Math.Log(N) / Math.Log(M));

                    // target number of root entries to maximize storage utilization
                    M = (int)Math.Ceiling((double)N / Math.Pow(M, height - 1));

                    items.Sort(xComparer);
                }

                node = new Node { Height = height };
                var N1 = (int)(Math.Ceiling((double)N / M) * Math.Ceiling(Math.Sqrt(M)));
                var N2 = (int)Math.Ceiling((double)N / M);
                var comparer = (level % 3) switch {
                    0 => xComparer,
                    1 => yComparer,
                    2 => zComparer,
                    _ => throw new NotSupportedException()
                };

                // split the items into M mostly square tiles
                for (var i = 0; i < N; i += N1) {
                    var slice = items.GetRange(i, Math.Min(N - i, N1));
                    slice.Sort(comparer);

                    for (var j = 0; j < slice.Count; j += N2) {
                        // pack each entry recursively
                        var childNode = BuildOneLevel(slice.GetRange(j, Math.Min(slice.Count - j, N2)), level + 1, height - 1);
                        node.Children.Add(childNode);
                    }
                }
            }
            RefreshEnvelope(node);

            return node;
        }

        public IList<Node> SearchAll(Collider collider) {
            var node = root;

            if (!collider.Intersects(node.BoundingBox)) return [];
            var retval = new List<Node>();
            var nodesToSearch = new Stack<Node>();

            while(node != null) {
                for(var i = 0; i < node.Children.Count; i++) {
                    var child = node.Children[i];
                    var childBox = child.BoundingBox;
                    if (collider.Intersects(childBox)) {
                        if (node.IsLeaf) retval.Add(child);
                        else if (collider.Contains(childBox)) Collect(child, retval);
                        else nodesToSearch.Push(child);
                    }
                }
                node = nodesToSearch.TryPop();
            }

            return retval;
        }
        public Node? SearchFirst(Ray ray) {
            var node = root;

            if ((ray.Intersects(node.BoundingBox) ?? float.PositiveInfinity) > float.MaxValue) return null;
            Node? retval = null;
            float nearestDistance = float.PositiveInfinity;
            var nodesToSearch = new Stack<Node>();

            while (node != null) {
                for (var i = 0; i < node.Children.Count; i++) {
                    var child = node.Children[i];
                    var childBox = child.BoundingBox;
                    var intersection = ray.Intersects(childBox) ?? float.PositiveInfinity;
                    if (intersection < float.PositiveInfinity) {
                        if (node.IsLeaf && intersection < nearestDistance) {
                            retval = child;
                            nearestDistance = intersection;
                        }
                        else nodesToSearch.Push(child);
                    }
                }
                node = nodesToSearch.TryPop();
            }

            return retval;
        }

        private static void Collect(Node node, List<Node> result) {
            var nodesToSearch = new Stack<Node>();
            while (node != null) {
                if (node.IsLeaf) result.AddRange(node.Children);
                else {
                    foreach (var n in node.Children)
                        nodesToSearch.Push(n);
                }

                node = nodesToSearch.TryPop();
            }
        }

        public void Clear() {
            root = new Node { IsLeaf = true, Height = 1 };
        }

        public bool Insert(Node item) => Insert(item, root.Height - 1);
        public bool Insert(T data, BoundingBox box) => Insert(new Node(data, box));
        public bool Insert(Node item, int level) {
            //Debug.Print($"Bounding box: {item.BoundingBox}");
            if (!item.BoundingBox.IsValid()) throw new ArgumentException("The bounding box contains NaN values");

            var box = item.BoundingBox;
            var insertPath = new List<Node>();
            // find the best node for accommodating the item, saving all nodes along the path too
            var node = ChooseSubtree(box, root, level, insertPath);

            // put the item into the node
            node.Children.Add(item);
            node.BoundingBox = BoundingBox.CreateMerged(node.BoundingBox, box);

            // split on node overflow; propagate upwards if necessary
            while (level >= 0) {
                if (insertPath[level].Children.Count <= maxEntries) break;

                Split(insertPath, level);
                level--;
            }

            // adjust bboxes along the insertion path
            AdjutsParentBounds(box, insertPath, level);

            return true;
        }

        private Node ChooseSubtree(BoundingBox bbox, Node node, int level, List<Node> path) {
            while (true) {
                path.Add(node);
                if (node.IsLeaf || path.Count - 1 == level) break;
                var minVolume = float.PositiveInfinity;
                var minEnlargement = float.PositiveInfinity;
                Node targetNode = null;
                Debug.Print("Scanning nodes");
                for(int i = 0; i < node.Children.Count; i++) {
                    var child = node.Children[i];
                    var volume = child.BoundingBox.Volume();
                    var combovolume = RTreeCalcs.CombinedVolume(bbox, child.BoundingBox);
                    var enlargement = combovolume - volume;
                    Debug.Print($"Enlargement: {enlargement}, combined volume: {combovolume}, volume: {volume} curr min: {minEnlargement}");

                    // choose entry with the least volume enlargement
                    if (enlargement < minEnlargement) {
                        minEnlargement = enlargement;
                        minVolume = MathF.Min(minVolume, volume);
                        targetNode = child;

                    } else if (enlargement == minEnlargement) {
                        // otherwise choose one with the smallest volume
                        if (volume < minVolume) {
                            minVolume = volume;
                            targetNode = child;
                        }
                    }
                }
                Debug.Assert(targetNode != null);
                node = targetNode;
            }
            return node;
        }

        private void Split(List<Node> insertPath, int level) {
            var node = insertPath[level];
            var totalCount = node.Children.Count;

            ChooseSplitAxis(node, minEntries, totalCount);

            var newNode = new Node { Height = node.Height };
            var splitIndex = ChooseSplitIndex(node, minEntries, totalCount);

            newNode.Children.AddRange(node.Children.GetRange(splitIndex, node.Children.Count - splitIndex));
            node.Children.RemoveRange(splitIndex, node.Children.Count - splitIndex);

            if(node.IsLeaf) newNode.IsLeaf = true;

            RefreshEnvelope(node);
            RefreshEnvelope(newNode);

            if (level > 0) insertPath[level - 1].Children.Add(newNode);
            else SplitRoot(node, newNode);
        }

        private void SplitRoot(Node node, Node newNode) {
            root = new Node {
                Children = { node, newNode },
                Height = node.Height + 1
            };
            RefreshEnvelope(root);
        }

        private int ChooseSplitIndex(Node node, int minEntries, int totalCount) {
            var minOverlap = float.MaxValue;
            var minVolume = float.MaxValue;
            int index = 0;
            for(int i = 0; i <= totalCount - minEntries; i++) {
                var bbox1 = SumChildBounds(node, 0, i);
                var bbox2 = SumChildBounds(node, i, totalCount);
                var overlap = RTreeCalcs.IntersectionVolume(bbox1, bbox2);
                var volume = bbox1.Volume() + bbox2.Volume();

                // choose distribution with minimum overlap
                if (overlap < minOverlap) {
                    minOverlap = overlap;
                    index = i;
                    minVolume = volume < minVolume ? volume : minVolume;
                } else if (overlap == minOverlap) {
                    // otherwise choose distribution with minimum volume
                    if (volume < minVolume) {
                        minVolume = volume;
                        index = i;
                    }
                }
            }
            return index;
        }

        public bool Remove(T item, BoundingBox boundingBox) {
            var node = root;
            var itemBox = boundingBox;
            var path = new Stack<Node>();
            var indexes = new Stack<int>();
            var i = 0;
            var goingUp = false;
            Node parent = null;

            while(node != null || path.Count > 0) {
                if(node == null) {
                    //go up
                    node = path.TryPop();
                    parent = path.TryPeek();
                    i = indexes.TryPop();
                    goingUp = true;
                }
                if(node != null && node.IsLeaf) {
                    // check current node
                    var index = node.Children.FindIndex(n => Comparer.Equals(item, n.Data));
                    if (index != -1) {
                        // item found, remove the item and condense tree upwards
                        node.Children.RemoveAt(index);
                        path.Push(node);
                        CondenseNodes(path.ToArray());
                        return true;
                    }
                }
                if (!goingUp && !node.IsLeaf && node.BoundingBox.Contains(itemBox) == ContainmentType.Contains) {
                    // go down
                    path.Push(node);
                    indexes.Push(i);
                    i = 0;
                    parent = node;
                    node = node.Children[0];
                } else if (parent != null) {
                    i++;
                    if (i == parent.Children.Count) {
                        // end of list; will go up
                        node = null;
                    } else {
                        // go right
                        node = parent.Children[i];
                        goingUp = false;
                    }

                } else node = null; // nothing found
            }
            return false;
        }

        private void CondenseNodes(IList<Node> path) {
            // go through the path, removing empty nodes and updating bboxes
            for (var i = path.Count - 1; i >= 0; i--) {
                if (path[i].Children.Count == 0) {
                    if (i == 0) {
                        Clear();
                    } else {
                        var siblings = path[i - 1].Children;
                        siblings.Remove(path[i]);
                    }
                } else {
                    RefreshEnvelope(path[i]);
                }
            }
        }

        // calculate node's bbox from bboxes of its children
        private static void RefreshEnvelope(Node node) {
            node.BoundingBox = SumChildBounds(node, 0, node.Children.Count);
        }

        private static BoundingBox SumChildBounds(Node node, int startIndex, int endIndex) {
            var retval = new BoundingBox();
            for (var i = startIndex; i < endIndex; i++)
                retval = BoundingBox.CreateMerged(node.Children[i].BoundingBox, retval);
            return retval;
        }

        private static void AdjutsParentBounds(BoundingBox bbox, List<Node> path, int level) {
            // adjust bboxes along the given tree path
            for (var i = level; i >= 0; i--) {
                path[i].BoundingBox = BoundingBox.CreateMerged(bbox, path[i].BoundingBox);
            }
        }

        // sorts node children by the best axis for split
        private static void ChooseSplitAxis(Node node, int m, int M) {
            var xMargin = AllDistMargin(node, m, M, xComparer);
            var yMargin = AllDistMargin(node, m, M, yComparer);
            var zMargin = AllDistMargin(node, m, M, zComparer);

            var comparer = xComparer;
            if( yMargin > xMargin) {
                if(yMargin > zMargin) comparer = yComparer;
                else comparer = zComparer;
            }else if (yMargin > zMargin) comparer = yComparer;

            // if total distributions margin value is minimal for x, sort by minX,
            // otherwise it's already sorted by minY
            node.Children.Sort(comparer);
        }

        private static float AllDistMargin(Node node, int m, int M, IComparer<Node> compare) {
            node.Children.Sort(compare);

            var leftBBox = SumChildBounds(node, 0, m);
            var rightBBox = SumChildBounds(node, M - m, M);
            var margin = leftBBox.Area() + rightBBox.Area();

            for (var i = m; i < M - m; i++) {
                var child = node.Children[i];
                leftBBox = BoundingBox.CreateMerged(leftBBox, child.BoundingBox);
                margin += leftBBox.Area();
            }

            for (var i = M - m - 1; i >= m; i--) {
                var child = node.Children[i];
                rightBBox = BoundingBox.CreateMerged(rightBBox, child.BoundingBox);
                margin += rightBBox.Area();
            }

            return margin;
        }
    }

    public static class RTreeCalcs {
        public static Comparer<RTreeBroken<T>.Node> CreateNodeComparer<T>(Func<Vector3, float> axisFn) {
            return Comparer<RTreeBroken<T>.Node>.Create((x, y) => {
                var pos1 = x.BoundingBox.Min;
                var pos2 = y.BoundingBox.Min;
                var num1 = axisFn(pos1);
                var num2 = axisFn(pos2);
                return num1.CompareTo(num2);
            });
        }

        public static float Volume(this BoundingBox bounds) {
            var size = bounds.Max - bounds.Min;
            return size.X * size.Y * size.Z;
        }
        public static float Area(this BoundingBox bounds) {
            var size = bounds.Max - bounds.Min;
            return ((size.X + size.Y) * size.Z) * 2 + (size.X * size.Y * 2);
        }
        public static float CombinedVolume(this BoundingBox bounds, BoundingBox other) => BoundingBox.CreateMerged(bounds, other).Volume();
        public static float IntersectionVolume(this BoundingBox bounds, BoundingBox other) => bounds.Intersection(other).Volume();
        public static BoundingBox Intersection(this BoundingBox a, BoundingBox b) {
            var min = Vector3.Max(a.Min, b.Min);
            var max = Vector3.Min(a.Max, b.Max);
            return new BoundingBox(min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T TryPop<T>(this Stack<T> stack) {
            return stack.Count == 0 ? default(T) : stack.Pop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T TryPeek<T>(this Stack<T> stack) {
            return stack.Count == 0 ? default(T) : stack.Peek();
        }

        public static bool IsValid(this BoundingBox box) => box.Max.IsValid() && box.Min.IsValid();
        public static bool IsValid(this Vector3 v) => float.IsRealNumber(v.X) && float.IsRealNumber(v.Y) && float.IsRealNumber(v.Z);
    }
}
