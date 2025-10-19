using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collections.Pooled;
using Microsoft.Xna.Framework;
using TranSimCS.Spatial;

namespace TranSimCS.RTree {
    public class RTree<T> {
        public struct Row(T element, BoundingBox boundingBox) {
            public T Element = element;
            public BoundingBox BoundingBox = boundingBox;
        }
        public delegate void LoadMethod(IEnumerable<Row>? data);


        public readonly int minChildren;
        public readonly int maxChildren;
        public Node<T> Root { get; internal set; }
        

        public RTree(IEnumerable<Row>? data = null, LoadMethod? method = null, int maxChildren = 9, int minChildren = 0) {
            this.maxChildren = maxChildren;
            this.minChildren = (minChildren < 2) ? (int)Math.Ceiling(maxChildren * 0.4) : minChildren;
            this.minChildren = Math.Min(minChildren, 2);
            var method0 = method ?? RStar;
            Clear();
            if(data != null) method0(data);
        }

        public void Clear() => Root = new Node<T>([]);
        public void Insert(T value, BoundingBox box) {
            box.EnsureValid();
            var node = new Node<T>(value, box);
            var stack = new Stack<Node<T>>([Root]);
            Insert0(stack, node);
        }
        private void Insert0(Stack<Node<T>> stack, Node<T> toInsert) {
            var boundsToInsert = toInsert.BoundingBox;
            var node = stack.Peek();
            while (stack.Count > 0) {
                node = stack.Peek();
                if(node == null) {
                    //It's the first insertion
                    Root = new Node<T>([toInsert]);
                    return;
                }
                if (node.IsLeaf) {
                    stack.Pop(); //end then continue
                } else {
                    //Find the best node to insert into
                    var minEnlargement = float.PositiveInfinity;
                    var minUpvolume = float.PositiveInfinity;
                    Node<T> minEnlargementNode = null;
                    var volume = node.BoundingBox.Volume();
                    var area = node.BoundingBox.Area();
                    foreach(var child in node.Children) {
                        if (child == null) throw new NullReferenceException("null child node");
                        if (child.IsLeaf) continue;
                        var newBoundingBox = BoundingBox.CreateMerged(boundsToInsert, child.BoundingBox);
                        var enlargement = newBoundingBox.Area() - area;
                        var upvolume = newBoundingBox.Volume() - volume;
                        if (enlargement < minEnlargement) {
                            minEnlargement = enlargement;
                            minUpvolume = upvolume;
                            minEnlargementNode = child;
                        } else if (enlargement == minEnlargement && upvolume < minUpvolume) {
                            minEnlargementNode = child;
                            minUpvolume = upvolume;
                        }
                    }
                    if (minEnlargementNode == null){
                        //All children are leaves, insert here
                        break;
                    }
                    stack.Push(minEnlargementNode);
                }
            }

            //Found a node to insert into
            while (stack.Count > 0) {
                node = stack.Peek();
                node.Children.Add(node);
                node.RecalcBounds();

                if (node.Children.Count > maxChildren) {
                    //Split the node
                    if(node == Root) {
                        //Splitting a root node
                    }
                } else return; //No need to split
            }
        }
    }

    public static class RTreeExtensions {
        public static void EnsureValid(this BoundingBox box, string? name = null) {
            if (!box.IsValid()) throw new ArgumentException("The bounding box contains NaN values", name);
        }
    }
}
