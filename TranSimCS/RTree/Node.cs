using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Spatial;

namespace TranSimCS.RTree {
    /// <summary>
    /// A node used by <see cref="RTree"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Node<T> {
        public readonly T Value;
        public readonly List<Node<T>> Children;
        public BoundingBox BoundingBox;

        public Node(T value, BoundingBox boundingBox) {
            Value = value;
            Children = new List<Node<T>>();
            BoundingBox = boundingBox;
        }
        public Node(IEnumerable<Node<T>> children) {
            Value = default;
            Children = children.ToList();
            RecalcBounds();
        }

        public bool IsLeaf => Children.Count == 0;

        public void RecalcBounds() {
            if (IsLeaf) return;
            var bounds = new BoundingBox();
            foreach (var child in Children) {
                bounds = BoundingBox.CreateMerged(bounds, child.BoundingBox);
            }
        }
        //LOOKUP
        public List<Node<T>> Find(BoundingBox box, bool mustBeEnclosed = false) {
            if (!box.IsValid()) return []; //Boxes containing NaN are not valid

            var intersection = box.Contains(BoundingBox);
            if (intersection == ContainmentType.Disjoint) return [];  //Disjoint
            if (IsLeaf) {
                if (intersection == ContainmentType.Contains) return [this]; //Fully enclosed by the lookup
                if (!mustBeEnclosed && intersection == ContainmentType.Intersects) return [this]; //Intersects and can be unenclosed
                return []; //Intersects, but must be enclosed
            }

            var list = new List<Node<T>>(Children.Count);
            foreach (var child in Children) {
                list.AddRange(child.Find(box, mustBeEnclosed));
            }
            return list;
        }

    }
}
