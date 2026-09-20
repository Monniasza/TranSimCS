using System;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Mode.RoadBuilder {
    /// <summary>
    /// A pickable region of the Road Builder's cross-section preview: either an existing lane of the
    /// draft, or the gap between two lanes where a new lane could be inserted.
    /// <para>
    /// This is the Road Builder's replacement for <see cref="Select.AddLaneSelection"/>. That type could
    /// only describe the two outermost positions (a side and a position), which is why the old tool could
    /// only ever append at the outside. A <see cref="LaneHit"/> names a lane by <see cref="LaneId"/> and
    /// carries the exact cross-section coordinate of the hit, so "insert between lane 2 and 3" is
    /// unambiguous.
    /// </para>
    /// <para>
    /// Instances are attached to the preview's triangles with <c>Mesh.AddTagsToLastTriangles</c> and come
    /// back out of the picker as <c>Selection.Tag</c>.
    /// </para>
    /// </summary>
    public readonly struct LaneHit : IRoadElement {
        /// <summary>
        /// The lane under the cursor. Only meaningful when <see cref="IsInsertion"/> is
        /// <see langword="false"/>.
        /// </summary>
        public readonly LaneId Id;

        /// <summary>
        /// The lane's index in the draft, 0 being leftmost. For an insertion hit this is the index the
        /// new lane would take, so it is the index of the lane to the right of the gap.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// The cross-section coordinate of the hit, in the draft's own left-to-right space. This is what
        /// makes an insertion point unambiguous.
        /// </summary>
        public readonly float Offset;

        /// <summary>The node end the preview is drawn at, so the tool knows which end is being edited.</summary>
        public readonly RoadNodeEnd NodeEnd;

        /// <summary>
        /// True when the cursor is over the gap between two lanes rather than over a lane itself. An
        /// insertion hit inserts at <see cref="Index"/>; a lane hit selects <see cref="Id"/>.
        /// </summary>
        public readonly bool IsInsertion;

        public LaneHit(LaneId id, int index, float offset, RoadNodeEnd nodeEnd, bool isInsertion = false) {
            Id = id;
            Index = index;
            Offset = offset;
            NodeEnd = nodeEnd;
            IsInsertion = isInsertion;
        }

        /// <summary>
        /// A hit on the gap at <paramref name="index"/>, where a new lane would be inserted.
        /// </summary>
        public static LaneHit Insertion(int index, float offset, RoadNodeEnd nodeEnd)
            => new(default, index, offset, nodeEnd, isInsertion: true);

        /// <summary>
        /// The cross-section extent a lane of the given width would occupy if inserted at this hit.
        /// </summary>
        public Interval<float> CalculateOffsets(float width) => new(Offset - width / 2, Offset + width / 2);

        //IRoadElement
        public Guid Guid => throw new NotSupportedException(
            "A LaneHit is a transient pick result, not a world object, so it has no Guid.");

        public int ZDiscriminant() => NodeEnd.ZDiscriminant();
        public int XDiscriminant() => 0;
        public LaneStrip? GetLaneStrip() => null;
        public RoadStrip? GetRoadStrip() => null;
        public RoadNode GetRoadNode() => NodeEnd.Node;
        public Lane? GetLane() => null;
        public HalfLane? GetLaneEnd() => null;
        public RoadNodeEnd GetNodeEnd() => NodeEnd;
        public int? GetIndexInHalfNode() => Index;

        public override string ToString()
            => IsInsertion
                ? $"LaneHit(insert at {Index}, offset {Offset})"
                : $"LaneHit({Id} at {Index}, offset {Offset})";
    }
}
