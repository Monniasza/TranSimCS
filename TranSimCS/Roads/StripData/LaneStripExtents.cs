using System;
using System.Collections.Immutable;
using System.Linq;
using TranSimCS.Roads.Range;
using TranSimCS.Roads.StripData;

public readonly struct LaneStripExtents {
    public LaneStripExtents(
        ImmutableHashSet<LaneStripData> leftBounds,
        ImmutableHashSet<LaneStripData> rightBounds,
        ImmutableArray<LaneStripExtent> extents) {
        LeftEdgeStrips = leftBounds;
        RightEdgeStrips = rightBounds;
        Extents = extents;
    }

    public ImmutableHashSet<LaneStripData> LeftEdgeStrips { get; }
    public ImmutableHashSet<LaneStripData> RightEdgeStrips { get; }
    public ImmutableArray<LaneStripExtent> Extents { get; }
}
public readonly struct LaneStripExtent {
    public ImmutableArray<LaneStripData> LaneStrips { get; }
    public DualRange Bounds { get; }

    public LaneStripExtent(
        ImmutableArray<LaneStripData> laneStrips) {
        LaneStrips = laneStrips;
        Bounds = laneStrips.Select(x => x.Bounds).Aggregate((x, y) => x | y);
    }
}
public struct LaneStripExtentIndex : IEquatable<LaneStripExtentIndex> {
    public int IndexOfExtent;
    public int LaneIndexInExtent;

    public LaneStripExtentIndex(int indexOfExtent, int laneIndexInExtent) {
        IndexOfExtent = indexOfExtent;
        LaneIndexInExtent = laneIndexInExtent;
    }

    public override bool Equals(object? obj) {
        return obj is LaneStripExtentIndex index && Equals(index);
    }

    public bool Equals(LaneStripExtentIndex other) {
        return IndexOfExtent == other.IndexOfExtent &&
               LaneIndexInExtent == other.LaneIndexInExtent;
    }

    public override int GetHashCode() {
        return HashCode.Combine(IndexOfExtent, LaneIndexInExtent);
    }

    public static bool operator ==(LaneStripExtentIndex left, LaneStripExtentIndex right) {
        return left.Equals(right);
    }

    public static bool operator !=(LaneStripExtentIndex left, LaneStripExtentIndex right) {
        return !(left == right);
    }
}