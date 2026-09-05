using System;
using System.Collections.Generic;

namespace TranSimCS.Tools.RoadConstruction {
    public struct RoadPresets : IEquatable<RoadPresets> {
        public int AddRemoveLeft;
        public int AddRemoveRight;
        public uint IncludeExcludeLeft;
        public uint IncludeExcludeRight;
        public bool IsInclusive;
        public DirectionChoice DirectionChoice;

        public bool IsInclineFlat;
        public bool IsTiltFlat;
        public float Height;
        public float HeightStep;
        public Alignment Alignment;
        public RoadMode RoadMode;

        public override bool Equals(object? obj) {
            return obj is RoadPresets presets && Equals(presets);
        }

        public bool Equals(RoadPresets other) {
            return AddRemoveLeft == other.AddRemoveLeft &&
                   AddRemoveRight == other.AddRemoveRight &&
                   IncludeExcludeLeft == other.IncludeExcludeLeft &&
                   IncludeExcludeRight == other.IncludeExcludeRight &&
                   IsInclusive == other.IsInclusive &&
                   DirectionChoice == other.DirectionChoice &&
                   IsInclineFlat == other.IsInclineFlat &&
                   IsTiltFlat == other.IsTiltFlat &&
                   Height == other.Height &&
                   HeightStep == other.HeightStep &&
                   Alignment == other.Alignment &&
                   EqualityComparer<RoadMode>.Default.Equals(RoadMode, other.RoadMode);
        }

        public override int GetHashCode() {
            HashCode hash = new HashCode();
            hash.Add(AddRemoveLeft);
            hash.Add(AddRemoveRight);
            hash.Add(IncludeExcludeLeft);
            hash.Add(IncludeExcludeRight);
            hash.Add(IsInclusive);
            hash.Add(DirectionChoice);
            hash.Add(IsInclineFlat);
            hash.Add(IsTiltFlat);
            hash.Add(Height);
            hash.Add(HeightStep);
            hash.Add(Alignment);
            hash.Add(RoadMode);
            return hash.ToHashCode();
        }

        public static bool operator ==(RoadPresets left, RoadPresets right) {
            return left.Equals(right);
        }

        public static bool operator !=(RoadPresets left, RoadPresets right) {
            return !(left == right);
        }
    }
}
