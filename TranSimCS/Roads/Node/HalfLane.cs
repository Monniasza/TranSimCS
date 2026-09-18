using System;
using System.Collections.Generic;
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
        public TrafficLightGroup? TrafficLight => GetAssignedRoadSection()?.TrafficLightGroup;

        /// <summary>
        /// Gets the road section that a car in this lane is about to enter, and which this lane's traffic light (if any) belongs to.
        /// </summary>
        public RoadSection? GetAssignedRoadSection() => OppositeHalf.HalfNode.ConnectedSection.Value;

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