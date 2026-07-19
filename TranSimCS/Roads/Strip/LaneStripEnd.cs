using System;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Strip {
    public struct LaneStripEnd(LaneStrip strip, SegmentHalf half) : IDraggableObj, IRoadElement {
        public LaneStrip strip = strip;
        public SegmentHalf half = half;
        public LaneEnd laneEnd => strip.GetHalf(half);

        //DRAGGING
        IPosition[] IDraggableObj.DraggableComponents() => ((IDraggableObj)strip).DraggableComponents();

        //ROAD ELEMENT
        public Guid Guid => strip.road.Guid;
        public Lane? GetLane() => strip.GetHalf(half).lane;
        public LaneStrip? GetLaneStrip() => strip;
        public RoadNode? GetRoadNode() => strip.GetHalf(half).RoadNodeEnd.Node;
        public RoadStrip? GetRoadStrip() => strip.road;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => half.Discriminant();
        public LaneEnd? GetLaneEnd() => laneEnd;
        public RoadNodeEnd? GetNodeEnd() => strip.GetHalf(half).RoadNodeEnd;

        public IPosition[] DraggableComponents() => ((IDraggableObj)strip).DraggableComponents();
    }
}
