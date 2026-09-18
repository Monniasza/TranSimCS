using System;
using System.Collections.Generic;
using System.Linq;
using Iesi.Collections.Generic;
using TranSimCS.Geometry;
using TranSimCS.Mode;
using TranSimCS.Property;
using TranSimCS.Roads.Section;
using TranSimCS.Roads.Strip;
using TranSimCS.TrafficLights;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Node {
    public class HalfLane: IRoadElement, IDraggableObj, ILaneSpec, IDemolish {
        //Definition
        public Lane Lane { get; private set; }
        public NodeEnd End { get; private set; }

        //Connections. Maintained by consumers
        internal HashSet<LaneStripEnd> _connectedLaneStrips;
        public ReadOnlySet<LaneStripEnd> ConnectedLaneStrips { get; private set; }

        //Contents
        public Property<LaneDefinition> DefinitionProp { get; private set; }
        public LaneDefinition Definition {
            get => DefinitionProp.Value;
            set => DefinitionProp.Value = value;
        }
        public LaneNode LaneNode => new(Definition, Lane.Guid);
        public LaneSpec LaneSpec {
            get => Definition.LaneSpec;
            set => Definition = new(MiddlePosition, value);
        }

        //Traffic lights. Attached to the road section this lane leads into (see GetAssignedRoadSection), not to the lane itself.
        public TrafficLightGroup? TrafficLight => HasTrafficLight ? GetAssignedRoadSection()?.TrafficLightGroup : null;

        /// <summary>
        /// Gets the road section that a car in this lane is about to enter, and which this lane's traffic light (if any) belongs to.
        /// </summary>
        public RoadSection? GetAssignedRoadSection() => OppositeHalf.HalfNode.ConnectedSection.Value;

        /// <summary>
        /// Whether a car can actually arrive at this half-lane via a connected lane strip. A strip's traffic
        /// flows from its StartLane to its EndLane, so this half-lane only ever receives cars if it is the
        /// End side of at least one connected strip. A light placed on a half-lane with no incoming strip
        /// would never be seen by any car, so such half-lanes are not eligible for HasTrafficLight.
        /// </summary>
        public bool HasIncomingLaneStrip => ConnectedLaneStrips.Any(strip => strip.half == SegmentHalf.End);

        //Whether this half-lane should be given a traffic light when its assigned road section has a traffic light group.
        //Not every half-lane needs a light, so this is an opt-in toggle, and only half-lanes with an incoming lane strip are eligible.
        //Bidirectional association: toggling this keeps HalfLane.HasTrafficLight and RoadSection.LanesWithTrafficLights in sync.
        public Property<bool> HasTrafficLightProp { get; private set; }
        public bool HasTrafficLight {
            get => HasTrafficLightProp.Value;
            set => HasTrafficLightProp.Value = value;
        }

        private void HasTrafficLightProp_ValueChanged(IProperty<bool> property, bool oldValue, bool newValue) {
            var section = GetAssignedRoadSection();
            if (newValue) section?.OnLaneTrafficLightAdded(this);
            else section?.OnLaneTrafficLightRemoved(this);

            //Invalidate the traffic light group's generated geometry, if any
            var group = section?.TrafficLightGroup;
            group?.FireDependencyEvent(group, Lane, PropertyNames.TrafficLightToggleOfLaneSuffix);
        }

        public bool IsPassingAllowed() {
            var lights = TrafficLight;
            if (lights == null) return true;
            if(lights.Phases.Count == 0) return true;
            var currentPhase = lights.Phases[lights.PhaseId];
            return currentPhase.GreenLanes.Contains(this);
        }

        //The constructor
        internal HalfLane(Lane lane, NodeEnd end) {
            Lane = lane;
            End = end;
            DefinitionProp = end.GetConditional(lane.InverseDefinitionProp, lane.DefinitionProp);
            _connectedLaneStrips = new();
            ConnectedLaneStrips = new(_connectedLaneStrips);
            HasTrafficLightProp = new(false, Guid + PropertyNames.TrafficLightToggleOfLaneSuffix, null);
            HasTrafficLightProp.ValueChanged += HasTrafficLightProp_ValueChanged;
        }

        //Derived properties
        public RoadNode RoadNode => Lane.RoadNode;
        public HalfNode HalfNode => Lane.RoadNode.GetHalfNode(End);
        public HalfLane OppositeHalf => End.GetConditional(Lane.FrontHalf, Lane.RearHalf);
        public int Index => (End == NodeEnd.Forward) ? Lane.Index : Lane.RoadNode.Lanes.Count - Lane.Index - 1;
        public float MiddlePosition { // Middle position of the lane, calculated as the average of left and right positions
            get => LaneNode.CenterPos;
            set => Definition = new(value, LaneSpec);
        } 
        public float Width {
            get => LaneNode.LaneSpec.Width;
            set => LaneSpec = LaneSpec with { Width = value };
        }
        public Interval<float> Bounds{
            get => LaneNode.Bounds;
            set => Definition = Definition.WithBounds(value);
        }
        public Guid Guid => Lane.Guid;
        
        public int ZDiscriminant() => End.Discriminant();
        public int XDiscriminant() => 0;
        public LaneStrip? GetLaneStrip() => null;
        public RoadStrip? GetRoadStrip() => null;
        public RoadNode? GetRoadNode() => RoadNode;
        public Lane? GetLane() => Lane;
        public HalfLane? GetLaneEnd() => this;
        public RoadNodeEnd? GetNodeEnd() => HalfNode.RoadNodeEnd;
        int? IRoadElement.GetIndexInHalfNode() => Index;
        public IPosition[] DraggableComponents() => [HalfNode];

        public void Demolish() => Lane.Demolish();
    }
}