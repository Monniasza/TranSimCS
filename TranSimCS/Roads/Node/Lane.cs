using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using TranSimCS.Property;
using TranSimCS.Roads;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Node {
    /// <summary>
    /// A lane defines where vehicles can ride through and in which direction.
    /// </summary>
    public class Lane: IDraggableObj, IRoadElement {
        //Contents
        public readonly Property<LaneNode> DefinitionProp;
        public LaneNode Definition { get => DefinitionProp.Value; set => DefinitionProp.Value = value.WithGUID(DefinitionProp.Value.ID); }
        public RoadNode RoadNode { get; internal set; }

        //Derived/cached values
        public Guid Guid => DefinitionProp.Value.ID;
        /// <summary>
        /// Specification of the lane, including properties like color, type, etc.
        /// The width here is ignored when set, but it's returned with the proper value when get.
        /// </summary>
        public LaneSpec Spec {
            get => Definition.LaneSpec;
            set => Definition = Definition.WithSpec(value);
        }
        public float LeftPosition { // Left position of the lane relative to the road node
            get => Definition.Bounds.Min;
            set => Definition = Definition.WithBounds(new(value, RightPosition)); 
        } 
        public float RightPosition { // Right position of the lane relative to the road node
            get => Definition.Bounds.Max;
            set => Definition = Definition.WithBounds(new(LeftPosition, value)); 
        } 
        public Range<float> Bounds {
            get => Definition.Bounds;
            set => Definition = Definition.WithBounds(value);
        }
        public int Index { get; internal set; } // Index of the lane in the road node's lane list
        public float MiddlePosition => Definition.CenterPos; // Middle position of the lane, calculated as the average of left and right positions
        public float Width => Definition.LaneSpec.Width;
        public LaneEnd Rear => new LaneEnd(NodeEnd.Backward, this);
        public LaneEnd Front => new LaneEnd(NodeEnd.Forward, this);

        public Lane(RoadNode node, LaneNode definition) {
            ArgumentNullException.ThrowIfNull(definition, nameof(definition));
            ArgumentNullException.ThrowIfNull(node, nameof(node));
            DefinitionProp = new(definition, "definition", node);
            DefinitionProp.ValidateChanges += (s, e) => {
                ArgumentNullException.ThrowIfNull(e.NewValue, nameof(definition));
            };
            DefinitionProp.ValueChanged += (s, e) => {
                node.Mesh.Invalidate();
            };
        }
        //Positioning utilities
        public LaneEnd GetEnd(NodeEnd end) => end.GetConditional(Rear, Front);

        //Indexing
        internal ISet<LaneStrip> connections = new HashSet<LaneStrip>(); // Set of lane strips that this lane is connected to
        public ISet<LaneStrip> Connections => new ReadOnlySet<LaneStrip>(connections); // Read-only set of lane strips that this lane is connected to

        //Dragging
        public void Drag(Vector3 vector, Vector3 dragFrom) => ((IDraggableObj)RoadNode).Drag(vector, dragFrom);
        public void Rotate(int azimuth, float incline, float tilt) => ((IDraggableObj)RoadNode).Rotate(azimuth, incline, tilt);

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
    }
}
