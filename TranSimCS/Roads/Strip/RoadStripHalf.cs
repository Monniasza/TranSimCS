using System;
using System.Collections.Generic;

namespace TranSimCS.Roads.Strip {
    public struct RoadStripHalf : IEquatable<RoadStripHalf> {
        public RoadStrip RoadStrip;
        public SegmentHalf SegmentHalf;

        public RoadStripHalf(RoadStrip roadStrip, SegmentHalf segmentHalf) {
            RoadStrip = roadStrip;
            SegmentHalf = segmentHalf;
        }
        public RoadStripHalf OppositeHalf() => new(RoadStrip, SegmentHalf.Inverse());

        public override bool Equals(object? obj) {
            return obj is RoadStripHalf half && Equals(half);
        }

        public bool Equals(RoadStripHalf other) {
            return EqualityComparer<RoadStrip>.Default.Equals(RoadStrip, other.RoadStrip) &&
                   SegmentHalf == other.SegmentHalf;
        }

        public override int GetHashCode() {
            return HashCode.Combine(RoadStrip, SegmentHalf);
        }

        public static bool operator ==(RoadStripHalf left, RoadStripHalf right) {
            return left.Equals(right);
        }

        public static bool operator !=(RoadStripHalf left, RoadStripHalf right) {
            return !(left == right);
        }
    }
}
