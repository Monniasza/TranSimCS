using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using TranSimCS.Property;
using TranSimCS.Roads.Section;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Node {
    /// <summary>
    /// A half of a road node, either front or back.
    /// </summary>
    public class HalfNode: IPosition {
        //Definition
        public RoadNode RoadNode { get; private set; }
        public NodeEnd End { get; private set; }
        public Property<PositionEulerAngles> PositionProp { get; private set; }

        //Connections. Maintained by consumers.
        internal HashSet<RoadStripHalf> _connectedRoadStrips;
        public ReadOnlySet<RoadStripHalf> ConnectedRoadStrips { get; private set; }

        //The constructor
        internal HalfNode(RoadNode roadNode, NodeEnd end) {
            RoadNode = roadNode;
            End = end;
            _connectedRoadStrips = new();
            ConnectedRoadStrips = new(_connectedRoadStrips);
            ConnectedSection = new Property<RoadSection?>(null, "connection");
            ConnectedSection.ValueChanged += ConnectedSection_ValueChanged;
            PositionProp = end.GetConditional(roadNode.InversePositionProp, roadNode.PositionProp);
        }

        //Contents
        public int LaneCount => RoadNode.Lanes.Count;
        public HalfLane GetLaneByIndex(int index) {
            if (End == NodeEnd.Backward) index = LaneCount - index - 1;
            return RoadNode.SortedLanes[index].GetHalfLane(End);
        }
        public HalfLane AddLane(LaneNode laneNode) {
            if (End == NodeEnd.Backward) laneNode = laneNode.Mirror;
            var lane = RoadNode.AddLane(laneNode);
            return lane.GetHalfLane(End);
        }
        public void Delete(HalfLane hlane) {
            if (hlane.End != End) throw new InvalidOperationException("The lane is assigned to the other end");
            RoadNode.RemoveLane(hlane.Lane);
        }
        public Property<RoadSection?> ConnectedSection { get; private set; }

        //Events
        public delegate void HalfLaneListener(HalfNode node, HalfLane lane);
        public event HalfLaneListener OnLaneAdded;
        public event HalfLaneListener OnLaneRemoved;
        internal void FireLaneAdded(Lane lane) => OnLaneAdded?.Invoke(this, lane.GetHalfLane(End));
        internal void FireLaneRemoved(Lane lane) => OnLaneRemoved?.Invoke(this, lane.GetHalfLane(End));

        //Derived properties
        public HalfNode OppositeHalf => End.GetConditional(RoadNode.FrontHalf, RoadNode.RearHalf);
        public RoadNodeEnd RoadNodeEnd => RoadNode.GetEnd(End);

        //Cached properties
        internal HalfNodeCache? _cache;
        public HalfNodeCache Cache => _cache ??= new HalfNodeCache(this);
        public NodeSpec NodeSpec => Cache.NodeSpec;
        public ImmutableArray<HalfLane> SortedLanes => Cache.SortedLanes;
        public Vector3 CenterPos => RoadNode.CenterPosition;
        public Range<float> Bounds => NodeSpec.Range;

        //Event listeners
        private void ConnectedSection_ValueChanged(object sender, RoadSection oldSection, RoadSection newSection) {
            var rne = RoadNodeEnd;
            oldSection?.OnDisconnect(rne);
            newSection?.OnConnect(rne);
        }
    }
}
