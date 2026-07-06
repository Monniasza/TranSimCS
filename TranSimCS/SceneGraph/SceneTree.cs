using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Collections;
using TranSimCS.Spatial;
using TranSimCS.Worlds;

namespace TranSimCS.SceneGraph {
    public sealed class SceneTree : SceneNode {
        private readonly List<SceneNode> children = new();

        public IReadOnlyList<SceneNode> Children => children;

        public void Add(SceneNode node) {
            if (children.Contains(node)) return;

            children.Add(node);
            node.Parent = this;

            RaiseAdded(node);

            // propagate upward if nested trees
            Parent?.RaiseAdded(node);
        }

        public void Remove(SceneNode node) {
            if (!children.Remove(node)) return;

            node.Parent = null;

            RaiseRemoved(node);

            Parent?.RaiseRemoved(node);
        }

        public override BoundingBox GetBounds() {
            if (children.Count == 0)
                return default;

            var box = children[0].GetBounds();
            for (int i = 1; i < children.Count; i++)
                box = BoundingBox.CreateMerged(box, children[i].GetBounds());

            return box;
        }
    }
}
