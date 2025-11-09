using System;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCS.Roads.Strip {
    public struct LaneRange(RoadStrip road, Lane startLaneIndexL, Lane startLaneIndexR, NodeEnd startSide, Lane endLaneIndexL, Lane endLaneIndexR, NodeEnd endSide): IRoadElement {
        public RoadStrip road = road; // The road connection this tag is associated with
        public Lane startLaneIndexL = startLaneIndexL; // The starting lane index for the tag
        public Lane startLaneIndexR = startLaneIndexR;
        public NodeEnd startSide = startSide;
        public Lane endLaneIndexL = endLaneIndexL;
        public Lane endLaneIndexR = endLaneIndexR;
        public NodeEnd endSide = endSide;

        //ROAD ELEMENT
        public Guid Guid => road.Guid;
        public Lane? GetLane() => null;
        public LaneStrip? GetLaneStrip() => null;
        public RoadNode? GetRoadNode() => null;
        public RoadStrip? GetRoadStrip() => road;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => 0;
        public LaneEnd? GetLaneEnd() => null;
        public RoadNodeEnd? GetNodeEnd() => null;
    }
}
