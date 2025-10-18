using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Worlds;

namespace TranSimCS.SceneGraph {
    public class SceneTree : SceneNode {
        protected override BoundingBox CalcBounds() {
            var bounds = new BoundingBox();
            foreach(var child in nodes) {
                bounds = BoundingBox.CreateMerged(bounds, child.GetBounds());
            }
            return bounds;
        }

        //CONTENTS
        private ISet<SceneNode> nodes = new HashSet<SceneNode>();
        public bool Add(SceneNode node) {
            var added = nodes.Add(node);
            if (!added) return false;
            node.Parent = this;
            Invalidate();
            return true;
        }
        public bool Remove(SceneNode node) {
            var removed = nodes.Remove(node);
            if (!removed) return false;
            node.Parent = null;
            Invalidate();
            return true;
        }
        public ISet<SceneNode> Nodes => new ReadOnlySet<SceneNode>(nodes);


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
