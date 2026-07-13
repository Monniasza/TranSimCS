using System;
using MonoGame.Extended;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Node {
    public struct LaneEnd(NodeEnd End, Lane Lane) : IEquatable<LaneEnd>, IDraggableObj, IRoadElement {
        //ROAD ELEMENT
        public Guid Guid => lane.Guid;
        public Lane GetLane() => lane;
        public LaneStrip? GetLaneStrip() => null;
        public RoadNode GetRoadNode() => lane.RoadNode;
        public RoadStrip? GetRoadStrip() => null;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => end.Discriminant();
        public LaneEnd? GetLaneEnd() => this;
        public RoadNodeEnd? GetNodeEnd() => RoadNodeEnd;


        //DRAGGING
        IPosition[] IDraggableObj.DraggableComponents() => [RoadNodeEnd];
        
        //DATA
        public NodeEnd end = End;
        public Lane lane = Lane;

        public RoadNodeEnd RoadNodeEnd => lane.RoadNode.GetEnd(end);
        public LaneEnd OppositeEnd => new LaneEnd(end.Negate(), lane);

        //BOUNDARIES, RESPECTING THE SIDE
        public (float Min, float Max, float Left, float Right) Boundaries() {
            var minMax = Range();
            float min = minMax.Min;
            float max = minMax.Max;
            float left = min;
            float right = max;
            if (end == NodeEnd.Backward) DataUtil.Swap(ref left, ref right);
            return (min, max, left, right);
        }
        public Range<float> Range() => lane.Bounds;


        //EQUALITY
        public override bool Equals(object? obj) =>
            obj is LaneEnd end && Equals(end);
        public bool Equals(LaneEnd other) =>
            end == other.end &&
            lane == other.lane;
        public override int GetHashCode() => HashCode.Combine(end, lane);

        public HalfLane ToHalfLane() => lane.GetHalfLane(end);

        public static bool operator ==(LaneEnd left, LaneEnd right) => left.Equals(right);
        public static bool operator !=(LaneEnd left, LaneEnd right) => !left.Equals(right);

    }
}
