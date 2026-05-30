using System;
using MonoGame.Extended;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCS.Roads.Strip {
    public struct LaneRange(RoadStrip road, Range<float> startRange, Range<float> endRange): IRoadElement {
        public RoadStrip road = road; // The road connection this tag is associated with
        public Range<float> startRange = startRange;
        public Range<float> endRange = endRange;

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
