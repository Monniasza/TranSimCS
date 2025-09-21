using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public struct LaneEnd(NodeEnd End, Lane Lane) : IEquatable<LaneEnd>, IDraggableObj {
        //DRAGGING
        public void Drag(Vector3 vector, Vector3 dragFrom) => lane.Drag(vector, dragFrom);

        public NodeEnd end = End;
        public Lane lane = Lane;

        public RoadNodeEnd RoadNodeEnd => lane.RoadNode.GetEnd(end);

        public LaneEnd OppositeEnd => new LaneEnd(end.Negate(), lane);

        public override bool Equals(object obj) {
            return obj is LaneEnd end && Equals(end);
        }

        public bool Equals(LaneEnd other) {
            return end == other.end &&
                   EqualityComparer<Lane>.Default.Equals(lane, other.lane);
        }

        public override int GetHashCode() {
            return HashCode.Combine(end, lane);
        }

        public static bool operator ==(LaneEnd left, LaneEnd right) {
            return left.Equals(right);
        }

        public static bool operator !=(LaneEnd left, LaneEnd right) {
            return !(left == right);
        }
    }
}
