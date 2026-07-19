using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using TranSimCS.Geometry;
using TranSimCS.Property;
using TranSimCS.Roads;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Node {
    /// <summary>
    /// A lane defines where vehicles can ride through and in which direction.
    /// </summary>
    public class Lane: Obj, IDraggableObj, IRoadElement {
        //Contents
        public Property<LaneDefinition> DefinitionProp { get; private set; }
        public BidirectionalDerivedProperty<LaneDefinition, LaneDefinition> InverseDefinitionProp { get; private set; }
        public LaneDefinition Definition { get => DefinitionProp.Value; set => DefinitionProp.Value = value; }
        public LaneNode LaneNode { get => new(Definition, Guid); set => DefinitionProp.Value = value.ToLaneDefinition; }
        public RoadNode RoadNode { get; internal set; }

        //Derived/cached values
        /// <summary>
        /// Specification of the lane, including properties like color, type, etc.
        /// The width here is ignored when set, but it's returned with the proper value when get.
        /// </summary>
        public LaneSpec Spec {
            get => Definition.LaneSpec;
            set => Definition = new(Definition.CenterPosition, value);
        }
        public Range<float> Bounds {
            get => Definition.Bounds();
            set {
                var newCenterPos = value.Middle();
                var newSpec = Spec;
                newSpec.Width = value.Width();
                Definition = new(newCenterPos, newSpec);
            }
        }
        public int Index => RoadNode.SortedLanes.IndexOf(this); // Index of the lane in the road node's lane list
        public float MiddlePosition => LaneNode.CenterPos; // Middle position of the lane, calculated as the average of left and right positions
        public float Width => LaneNode.LaneSpec.Width;
        public LaneEnd Rear => new LaneEnd(NodeEnd.Backward, this);
        public LaneEnd Front => new LaneEnd(NodeEnd.Forward, this);

        public Lane(RoadNode node, LaneNode definition) {
            Guid = definition.ID;
            ArgumentNullException.ThrowIfNull(definition, nameof(definition));
            ArgumentNullException.ThrowIfNull(node, nameof(node));
            DefinitionProp = new(definition.ToLaneDefinition, "definition", node);
            DefinitionProp.ValueChanged += (s, old, value) => {
                node.Mesh.Invalidate();
            };
            InverseDefinitionProp = new("invDefinition", DefinitionProp, LaneDefinitionMethods.Mirror, LaneDefinitionMethods.Mirror);
            FrontHalf = new(this, NodeEnd.Forward);
            RearHalf = new(this, NodeEnd.Backward);
        }
        //Positioning utilities
        public LaneEnd GetEnd(NodeEnd end) => end.GetConditional(Rear, Front);

        //Indexing
        internal ISet<LaneStrip> connections = new HashSet<LaneStrip>(); // Set of lane strips that this lane is connected to
        public ISet<LaneStrip> Connections => new ReadOnlySet<LaneStrip>(connections); // Read-only set of lane strips that this lane is connected to

        public HalfLane FrontHalf { get; private set; }
        public HalfLane RearHalf { get; private set; }
        public HalfLane GetHalfLane(NodeEnd nodeEnd) => nodeEnd.GetConditional(RearHalf, FrontHalf);

        //Dragging
        IPosition[] IDraggableObj.DraggableComponents() => [RoadNode];

        public int ZDiscriminant() {
            return 0;
        }

        public int XDiscriminant() {
            return 0;
        }

        public LaneStrip? GetLaneStrip() {
            return null;
        }

        public RoadStrip? GetRoadStrip() {
            return null;
        }

        public RoadNode? GetRoadNode() {
            return RoadNode;
        }

        public Lane? GetLane() {
            return this;
        }

        public LaneEnd? GetLaneEnd() {
            return null;
        }

        public RoadNodeEnd? GetNodeEnd() {
            return null;
        }

        int? IRoadElement.GetIndexInHalfNode() => Index;
    }
}
