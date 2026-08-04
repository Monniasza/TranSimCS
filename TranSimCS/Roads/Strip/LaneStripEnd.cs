using System;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Strip {
    public struct LaneStripEnd(LaneStrip strip, SegmentHalf half) : IDraggableObj, IRoadElement {
        public LaneStrip strip = strip;
        public SegmentHalf half = half;
        public HalfLane laneEnd => strip.GetHalf(half);

        //DRAGGING
        IPosition[] IDraggableObj.DraggableComponents() => ((IDraggableObj)strip).DraggableComponents();

        //ROAD ELEMENT
        public Guid Guid => strip.Road.Guid;
        public Lane? GetLane() => strip.GetHalf(half).Lane;
        public LaneStrip? GetLaneStrip() => strip;
        public RoadNode? GetRoadNode() => strip.GetHalf(half).RoadNode;
        public RoadStrip? GetRoadStrip() => strip.Road;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => half.Discriminant();
        public HalfLane? GetLaneEnd() => laneEnd;
        public RoadNodeEnd? GetNodeEnd() => strip.GetHalf(half).HalfNode.RoadNodeEnd;

        public IPosition[] DraggableComponents() => ((IDraggableObj)strip).DraggableComponents();
    }
}
