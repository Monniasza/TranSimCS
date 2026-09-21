using System;

namespace TranSimCS.Mode.RoadBuilder {
    /// <summary>
    /// A stable identity for a lane within a single <see cref="NodeSpecDraft"/>.
    /// <para>
    /// Lane edits are expressed as <c>(LaneId, operation)</c> rather than as positional indices, so
    /// inserting or removing a lane never invalidates an edit that refers to another lane. This is the
    /// property that makes inside-insertion, reordering and arbitrary merging possible.
    /// </para>
    /// <para>
    /// A <see cref="LaneId"/> is only meaningful within the draft that issued it. It is deliberately
    /// <b>not</b> a <see cref="Guid"/>: the draft is a tool-side concept and its identities must never
    /// leak into the world model or the save format.
    /// </para>
    /// </summary>
    public readonly struct LaneId : IEquatable<LaneId>, IComparable<LaneId> {
        public readonly int Value;

        public LaneId(int value) => Value = value;

        public bool Equals(LaneId other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is LaneId other && Equals(other);
        public override int GetHashCode() => Value;
        public int CompareTo(LaneId other) => Value.CompareTo(other.Value);
        public override string ToString() => $"LaneId({Value})";

        public static bool operator ==(LaneId left, LaneId right) => left.Equals(right);
        public static bool operator !=(LaneId left, LaneId right) => !left.Equals(right);
    }
}
