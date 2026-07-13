using System;
using MonoGame.Extended;

namespace TranSimCS.Roads.Node {
    public sealed class LaneNode: IComparable<LaneNode>, IEquatable<LaneNode>{
        public readonly Guid ID;
        public readonly LaneSpec LaneSpec;
        public readonly float CenterPos;
        public Range<float> Bounds { get; private set; }

        public LaneNode(LaneSpec laneSpec, float centerPos, Guid? guid = null) {
            ID = guid ?? Guid.NewGuid();
            this.LaneSpec = laneSpec;
            this.CenterPos = centerPos;
            Bounds = new(CenterPos - LaneSpec.Width / 2, CenterPos + LaneSpec.Width / 2);
        }
        public LaneNode(LaneDefinition laneDefinition, Guid? guid = null) : this(laneDefinition.LaneSpec, laneDefinition.CenterPosition, guid) { }
        public LaneDefinition ToLaneDefinition => new(CenterPos, LaneSpec);

        public LaneNode WithBounds(Range<float> bounds) {
            var laneSpec = LaneSpec;
            var centerPos = (bounds.Min + bounds.Max) /2;
            laneSpec.Width = bounds.Max - bounds.Min;
            return new LaneNode(laneSpec, centerPos);
        }

        public static LaneNode FromBounds(LaneSpec spec, Range<float> bounds, Guid? guid = null){
            spec.Width = bounds.Max - bounds.Min;
            var cpos = (bounds.Min + bounds.Max) / 2;
            return new(spec, cpos, guid);
        }

        public int CompareTo(LaneNode? other) {
            if(other == null) return 1;
            if(this == other) return 0;
            var compareCenters = CenterPos.CompareTo(other.CenterPos);
            if(compareCenters != 0) return compareCenters;
            var compareWidths = LaneSpec.Width.CompareTo(other.LaneSpec.Width);
            if(compareWidths != 0) return compareWidths;
            return ID.CompareTo(other.ID);
        }

        public bool Equals(LaneNode? other) {
            return other != null && this.ID == other.ID && this.LaneSpec == other.LaneSpec && this.CenterPos == other.CenterPos;
        }

        public LaneNode Mirror => new(LaneSpec, -CenterPos, ID);

        public static bool operator <(LaneNode left, LaneNode right) {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(LaneNode left, LaneNode right) {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(LaneNode left, LaneNode right) {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(LaneNode left, LaneNode right) {
            return left.CompareTo(right) >= 0;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (ReferenceEquals(obj, null)) {
                return false;
            }

            return Equals((LaneNode)obj);
        }

        public override int GetHashCode() {
            return HashCode.Combine(CenterPos, LaneSpec, ID);
        }

        public LaneNode WithGUID(Guid iD) {
            return new(LaneSpec, CenterPos, iD);
        }
        public LaneNode WithSpec(LaneSpec spec) {
            return new(spec, CenterPos, ID);
        }

        public static bool operator ==(LaneNode left, LaneNode right) {
            if (ReferenceEquals(left, null)) {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(LaneNode left, LaneNode right) {
            return !(left == right);
        }
    }
}
