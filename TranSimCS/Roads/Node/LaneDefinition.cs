using System;
using MonoGame.Extended;

namespace TranSimCS.Roads.Node {
    public struct LaneDefinition : IEquatable<LaneDefinition> {
        public float CenterPosition;
        public LaneSpec LaneSpec;

        public LaneDefinition(float centerPosition, LaneSpec laneSpec) {
            CenterPosition = centerPosition;
            LaneSpec = laneSpec;
        }

        public override bool Equals(object? obj) {
            return obj is LaneDefinition definition && Equals(definition);
        }

        public bool Equals(LaneDefinition other) {
            return CenterPosition == other.CenterPosition &&
                   LaneSpec.Equals(other.LaneSpec);
        }

        public override int GetHashCode() {
            return HashCode.Combine(CenterPosition, LaneSpec);
        }

        public static bool operator ==(LaneDefinition left, LaneDefinition right) {
            return left.Equals(right);
        }

        public static bool operator !=(LaneDefinition left, LaneDefinition right) {
            return !(left == right);
        }
    }
}
