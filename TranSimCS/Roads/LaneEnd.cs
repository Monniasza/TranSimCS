using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public struct LaneEnd(NodeEnd End, Lane Lane) : IEquatable<LaneEnd>, IDraggableObj {
        //DRAGGING
        public void Drag(Vector3 vector, Vector3 dragFrom) => lane.Drag(vector, dragFrom);
        public void Rotate(int fieldAzimuth, float pitch, float tilt) => lane.Rotate(fieldAzimuth, pitch, tilt);
        
        //DATA
        public NodeEnd end = End;
        public Lane lane = Lane;

        public RoadNodeEnd RoadNodeEnd => lane.RoadNode.GetEnd(end);

        public LaneEnd OppositeEnd => new LaneEnd(end.Negate(), lane);

        //BOUNDARIES, RESPECTING THE SIDE
        public Vector2 Boundaries() {
            Vector2 bounds = new(lane.LeftPosition, lane.RightPosition);
            if(end == NodeEnd.Backward) return bounds;
            return new Vector2(-bounds.Y, -bounds.X);
        }


        //EQUALITY
        public override bool Equals(object? obj) =>
            obj is LaneEnd end && Equals(end);
        public bool Equals(LaneEnd other) =>
            end == other.end &&
            lane == other.lane;
        public override int GetHashCode() => HashCode.Combine(end, lane);
        public static bool operator ==(LaneEnd left, LaneEnd right) => left.Equals(right);
        public static bool operator !=(LaneEnd left, LaneEnd right) => !left.Equals(right);

    }
}
