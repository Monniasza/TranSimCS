using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TranSimCS.Roads.Range;

public interface IExtent {
    public DualRange Bounds { get; }
}
public readonly struct Extents<T> where T: IExtent {
    public Extents(
        ImmutableHashSet<T> leftBounds,
        ImmutableHashSet<T> rightBounds,
        ImmutableArray<Extent<T>> extents) {
        LeftEdgeStrips = leftBounds;
        RightEdgeStrips = rightBounds;
        Groups = extents;

        var dictBuilder = new List<KeyValuePair<T, ExtentIndex>>();
        for (int i = 0; i < extents.Length; i++) {
            var extent = extents[i];
            for (int j = 0; j < extent.LaneStrips.Length; j++) {
                var laneStrip = extent.LaneStrips[j];
                dictBuilder.Add(new(laneStrip, new(i, j)));
            }
        }
        ElementToIndex = dictBuilder.ToImmutableDictionary();
    }

    public ImmutableHashSet<T> LeftEdgeStrips { get; }
    public ImmutableHashSet<T> RightEdgeStrips { get; }
    public ImmutableArray<Extent<T>> Groups { get; }
    public ImmutableDictionary<T, ExtentIndex> ElementToIndex { get; }
    public T this[ExtentIndex index] => Groups[index.IndexOfExtent].LaneStrips[index.LaneIndexInExtent];
}
public readonly struct Extent<T> where T : IExtent {
    public ImmutableArray<T> LaneStrips { get; }
    public DualRange Bounds { get; }

    public Extent(
        ImmutableArray<T> laneStrips) {
        LaneStrips = laneStrips;
        Bounds = laneStrips.Select(x => x.Bounds).Aggregate((x, y) => x | y);
    }
}
public struct ExtentIndex : IEquatable<ExtentIndex> {
    public int IndexOfExtent;
    public int LaneIndexInExtent;

    public ExtentIndex(int indexOfExtent, int laneIndexInExtent) {
        IndexOfExtent = indexOfExtent;
        LaneIndexInExtent = laneIndexInExtent;
    }

    public override bool Equals(object? obj) {
        return obj is ExtentIndex index && Equals(index);
    }

    public bool Equals(ExtentIndex other) {
        return IndexOfExtent == other.IndexOfExtent &&
               LaneIndexInExtent == other.LaneIndexInExtent;
    }

    public override int GetHashCode() {
        return HashCode.Combine(IndexOfExtent, LaneIndexInExtent);
    }

    public static bool operator ==(ExtentIndex left, ExtentIndex right) {
        return left.Equals(right);
    }

    public static bool operator !=(ExtentIndex left, ExtentIndex right) {
        return !(left == right);
    }
}