using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TranSimCS.Spatial {
    public partial class RTree<T> {
        /// <summary>
        /// R-tree node. Ported from https://github.com/marchello2000/RTree/blob/master/src/RTree/RTreeNode.cs#L10
        /// </summary>
        public class Node {
            private readonly Lazy<List<Node>> children;
            public T Data { get; private set; }
            public BoundingBox BoundingBox { get; internal set; }

            internal Node() : this(default(T), default(BoundingBox)) { }

            public Node(T data, BoundingBox boundingBox) {
                Height = 1;
                Data = data;
                BoundingBox = boundingBox;
                children = new Lazy<List<Node>>(() => new List<Node>(), System.Threading.LazyThreadSafetyMode.None);
            }

            internal bool IsLeaf { get; set; }
            internal int Height { get; set; }
            internal List<Node> Children => children.Value;
        }
    }
}
