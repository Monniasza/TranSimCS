using System.Collections.Generic;
using Iesi.Collections.Generic;
using TranSimCS.Property;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Roads.Node {
    public class HalfLane {
        //Definition
        public Lane Lane { get; private set; }
        public NodeEnd End { get; private set; }

        //Connections. Maintained by consumers
        internal HashSet<LaneStripEnd> _connectedLaneStrips;
        public ReadOnlySet<LaneStripEnd> ConnectedLaneStrips { get; private set; }

        //Contents
        public Property<LaneDefinition> DefinitionProp { get; private set; }
        public LaneDefinition Definition { get => DefinitionProp.Value; set => DefinitionProp.Value = value; }
        public LaneNode LaneNode => new(Definition, Lane.Guid);

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
    }
}