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
    public class SceneTree : SceneNode {
        protected override BoundingBox CalcBounds() {
            /*var bounds = new BoundingBox();
            foreach(var child in nodes) {
                bounds = BoundingBox.CreateMerged(bounds, child.GetBounds());
            }
            return bounds;*/
            foreach(var removal in diff.removals) {
                index.Remove(removal);
            }
            foreach (var addition in diff.additions) {
                index.Insert(addition, addition.GetBounds());
            }
            return index.Bounds();
        }

        //CONTENTS
        private ISet<SceneNode> nodes = new HashSet<SceneNode>();
        public bool Add(SceneNode node) {
            var added = nodes.Add(node);
            if (!added) return false;
            node.Parent = this;
            node.OnRebuild += NodeAdded;
            NodeAdded(node);
            Invalidate();
            return true;
        }
        public bool Remove(SceneNode node) {
            var removed = nodes.Remove(node);
            if (!removed) return false;
            node.Parent = null;
            node.OnRebuild -= NodeAdded;
            NodeRemoved(node);
            Invalidate();
            return true;
        }
        public ISet<SceneNode> Nodes => new ReadOnlySet<SceneNode>(nodes);

        //INDEX
        private readonly Diff<SceneNode> diff = new Diff<SceneNode>();
        private readonly RTree<SceneNode> index = new RTree<SceneNode>();
        private void NodeRemoved(SceneNode node) => diff.Remove(node);
        private void NodeAdded(SceneNode node) => diff.Add(node);

        protected override bool FindInternal(Ray ray, out SceneNode? node, out float dist, out object? tag) {
            var currDist = float.MaxValue;
            object? currTag = null;
            SceneNode? currNode = null;
            bool found = false;
            foreach(var child in Nodes) {
                if(child.Find(ray, out var nextNode, out var nextDist, out var nextTag)) 
                    if(nextDist < currDist) {
                        currDist = nextDist;
                        currTag = nextTag;
                        currNode = nextNode;
                        found = true;
                    }
            }
            node = currNode;
            dist = currDist;
            tag = currTag;
            return found;
        }
    }
}
