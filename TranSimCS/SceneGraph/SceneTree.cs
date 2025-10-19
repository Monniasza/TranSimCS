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
            foreach(var removal in removals) {
                index.Remove(removal.node, removal.box);
            }
            foreach (var addition in additions) {
                index.Insert(addition, addition.GetBounds());
            }
            additions.Clear();
            removals.Clear();
            return index.Bounds();
        }

        //CONTENTS
        private ISet<SceneNode> nodes = new HashSet<SceneNode>();
        public bool Add(SceneNode node) {
            var added = nodes.Add(node);
            if (!added) return false;
            node.Parent = this;
            node.OnRebuild += NodeAdded;
            node.OnInvalidate += NodeInvalidated;
            NodeAdded(node);
            Invalidate();
            return true;
        }
        public bool Remove(SceneNode node) {
            var removed = nodes.Remove(node);
            if (!removed) return false;
            node.Parent = null;
            node.OnRebuild -= NodeAdded;
            node.OnInvalidate -= NodeInvalidated;
            NodeRemoved(node);
            Invalidate();
            return true;
        }
        public ISet<SceneNode> Nodes => new ReadOnlySet<SceneNode>(nodes);

        //INDEX
        private readonly HashSet<SceneNode> additions = [];
        private readonly HashSet<SceneContainer> removals = [];

        private readonly RTree<SceneNode> index = new RTree<SceneNode>();
        private void NodeRemoved(SceneNode node) => removals.Add(new(node, node.GetBounds()));
        private void NodeAdded(SceneNode node) => additions.Add(node);
        private void NodeInvalidated(SceneNode node, BoundingBox box) {
            removals.Add(new(node, box));
            additions.Add(node);
        }

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
