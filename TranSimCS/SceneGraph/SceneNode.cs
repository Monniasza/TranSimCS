using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Model;
using TranSimCS.Spatial;
using TranSimCS.Worlds;

namespace TranSimCS.SceneGraph {
    /// <summary>
    /// An element of a scene graphs. There are two types of nodes: leaf nodes which attach to mesh generators and branches which combine several subnodes together.
    /// </summary>
    public abstract class SceneNode: IBVHElement {
        /// <summary>
        /// Abstract constructor called by subclasses of this class
        /// </summary>
        public SceneNode() {
            ActiveProp = new Property<bool>(true, "");
        }

        /// <summary>
        /// Property for the <see cref="Active"/>. Allows listening to changes in activation.
        /// </summary>
        public Property<bool> ActiveProp { get; }
        /// <summary>
        /// Is this scene node active? Set to false to temporarily disable this scene node and all of its children.
        /// </summary>
        public bool Active { get => ActiveProp.Value; set => ActiveProp.Value = value; }


        //SCENE GRAPHING
        /// <summary>
        /// The parent scene graph of this node. Set internally when this node is added <see cref="SceneTree.Add(SceneNode)"/> or removed <see cref="SceneTree.Remove(SceneNode)"/>
        /// </summary>
        public SceneTree? Parent { get; internal set; }

        //BOUNDS
        /// <summary>
        /// Called when the node is invalidated by <see cref="GetBounds"/>
        /// </summary>
        public event Action<SceneNode, BoundingBox>? OnInvalidate;
        /// <summary>
        /// Called when the node is rebuilt after invalidation <see cref="Invalidate"/> by the <see cref="GetBounds"/>
        /// </summary>
        public event Action<SceneNode>? OnRebuild;

        /// <summary>
        /// Invalides the current scene node and all of its ancestors
        /// </summary>
        public void Invalidate() {
            if (Bounds == null) return;
            var oldBounds = Bounds ?? default;
            Bounds = null;
            Parent?.Invalidate();
            OnInvalidate?.Invoke(this, oldBounds);
        }
        private BoundingBox? Bounds;
        /// <summary>
        /// Gets the bounding box of this scene node. Calls <see cref="OnRebuild"/> if the scene node or any of its children were changed or invalidated
        /// </summary>
        /// <returns></returns>
        public BoundingBox GetBounds() {
            if (Bounds == null) {
                Bounds = CalcBounds();
                OnRebuild?.Invoke(this);
            }
            return Bounds.Value;
        }
        /// <summary>
        /// Implement this to calculate the bounds. Event invocations are handled by the <see cref="GetBounds"/> method
        /// </summary>
        /// <returns>bounding box of this node</returns>
        protected abstract BoundingBox CalcBounds();

        /// <summary>
        /// Find an intersection with the given ray
        /// </summary>
        /// <param name="ray">ray to check against</param>
        /// <param name="node">set to the closest leaf node if an intersection is found, or <see cref="null"/> if not found</param>
        /// <param name="dist">set to the T value if an intersection is found, or <see cref="float.MaxValue"/> if not found</param>
        /// <param name="tag">set to the found tag if an intersection is found, or <see cref="null"/> if not found</param>
        /// <returns>does the ray intersect this node or its subcomponents?</returns>
        public bool Find(Ray ray, out SceneNode? node, out float dist, out object? tag){
            if (!ActiveProp.Value || GetBounds().Intersects(ray) == null) return SceneNode.Reject(this, out node, out dist, out tag);
            return FindInternal(ray, out node, out dist, out tag);
        }

        /// <summary>
        /// Implement this method to find the intersection. The activation and rough check have alredy been done.
        /// Follows the same rules as <see cref="Find(Ray, out SceneNode, out float, out object?)"/>
        /// </summary>
        /// <param name="ray">ray to check against</param>
        /// <param name="node">set to the closest leaf node if an intersection is found, or <see cref="null"/> if not found</param>
        /// <param name="dist">set to the T value if an intersection is found, or <see cref="float.MaxValue"/> if not found</param>
        /// <param name="tag">set to the found tag if an intersection is found, or <see cref="null"/> if not found</param>
        /// <returns>does the ray intersect this node or its subcomponents?</returns>
        protected abstract bool FindInternal(Ray ray, out SceneNode? node, out float dist, out object? tag);

        /// <summary>
        /// Used by <see cref="Find(Ray, out SceneNode, out float, out object?)"/> to reject rays that do not intersect with this scene graph
        /// </summary>
        /// <param name="that">source scene node</param>
        /// <param name="node">set to the current scene node</param>
        /// <param name="dist">set to <see cref="float.MaxValue"/></param>
        /// <param name="tag">set to <see cref="null"/></param>
        /// <returns></returns>
        public static bool Reject(SceneNode that, out SceneNode? node, out float dist, out object? tag) {
            node = that;
            dist = float.MaxValue;
            tag = null;
            return false;
        }

        public static BoundingBox FromMany(IEnumerable<IRenderBin> meshes) {
            var boundingBoxes = meshes.Select(MeshUtil.BoundingBox);
            return AggregateBounds(boundingBoxes);
        }
        public static BoundingBox AggregateBounds(IEnumerable<BoundingBox> boxes) => boxes.AggregateOrDefault(new BoundingBox(), BoundingBox.CreateMerged);

        public bool ComputeIntersection(Ray ray, out float distance, out object tag) => Find(ray, out var ignore1, out distance, out tag);
    }

    public struct SceneContainer(SceneNode node, BoundingBox box) {
        public SceneNode node = node;
        public BoundingBox box = box;
    }
}
