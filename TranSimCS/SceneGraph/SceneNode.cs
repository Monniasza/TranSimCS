using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Model;
using TranSimCS.Property;
using TranSimCS.Spatial;

namespace TranSimCS.SceneGraph {
    /// <summary>
    /// An element of a scene graphs. There are two types of nodes: leaf nodes which attach to mesh generators and branches which combine several subnodes together.
    /// </summary>
    public abstract class SceneNode {
        public SceneTree? Parent { get; internal set; }

        public Property<bool> Active { get; } = new(true, "");

        public event Action<SceneNode>? ChildAdded;
        public event Action<SceneNode>? ChildRemoved;

        protected void RaiseAdded(SceneNode node)
            => ChildAdded?.Invoke(node);

        protected void RaiseRemoved(SceneNode node)
            => ChildRemoved?.Invoke(node);

        public abstract BoundingBox GetBounds();
    }
}
