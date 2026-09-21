using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCS.Mode.RoadBuilder {
    /// <summary>
    /// One lane inside a <see cref="NodeSpecDraft"/>: a <see cref="LaneSpec"/> plus the identity that
    /// survives insertions and removals elsewhere in the draft.
    /// </summary>
    public sealed class LaneDraft {
        public readonly LaneId Id;
        public LaneSpec Spec;

        internal LaneDraft(LaneId id, LaneSpec spec) {
            Id = id;
            Spec = spec;
        }

        public LaneDraft Clone() => new(Id, Spec);
    }

    /// <summary>
    /// The editable document of the Road Builder: an ordered list of lanes, left to right, with a
    /// centreline. Everything the tool does is a mutation of this value; the world is only touched when
    /// the draft is committed.
    /// <para>
    /// Lane offsets are <b>derived</b> from the ordered list (see <see cref="ComputeOffsets"/>), never
    /// stored. That is what makes widening a lane in the middle push its neighbours outward instead of
    /// overlapping them, and what makes <see cref="Exit"/> able to add a lane beside another without
    /// moving it.
    /// </para>
    /// </summary>
    public sealed class NodeSpecDraft : IEnumerable<LaneDraft> {
        private readonly List<LaneDraft> lanes = [];
        private int nextId;

        /// <summary>
        /// Offset of the centre of the cross-section. Lanes are laid out symmetrically around this value,
        /// which is what makes asymmetric roads (e.g. three lanes one way, one the other) expressible
        /// without special-casing.
        /// </summary>
        public float Centerline { get; set; }

        /// <summary>
        /// When set, the total cross-section width is forced to this value and lane widths are scaled to
        /// fit. When <see langword="null"/>, the width is the sum of the lane widths.
        /// </summary>
        public float? ForcedWidth { get; set; }

        public NodeSpecDraft() { }

        public NodeSpecDraft(float centerline) => Centerline = centerline;

        //Contents
        public int Count => lanes.Count;
        public LaneDraft this[int index] => lanes[index];
        public IReadOnlyList<LaneDraft> Lanes => lanes;

        /// <summary>Total cross-section width: either the forced width or the sum of the lane widths.</summary>
        public float TotalWidth => ForcedWidth ?? lanes.Sum(x => x.Spec.Width);

        /// <summary>
        /// The cross-section extent of the draft, in the same coordinate space as
        /// <see cref="NodeSpec.Range"/>. Empty drafts report a degenerate interval at the centreline.
        /// </summary>
        public Interval<float> Range {
            get {
                if (lanes.Count == 0) return new(Centerline, Centerline);
                var offsets = ComputeOffsets();
                var min = float.PositiveInfinity;
                var max = float.NegativeInfinity;
                for (int i = 0; i < lanes.Count; i++) {
                    var half = lanes[i].Spec.Width / 2;
                    min = MathF.Min(min, offsets[i] - half);
                    max = MathF.Max(max, offsets[i] + half);
                }
                return new(min, max);
            }
        }

        //Lookup
        public int IndexOf(LaneId id) {
            for (int i = 0; i < lanes.Count; i++)
                if (lanes[i].Id == id) return i;
            return -1;
        }

        public bool Contains(LaneId id) => IndexOf(id) >= 0;

        public LaneDraft Get(LaneId id) {
            var index = IndexOf(id);
            if (index < 0) throw new KeyNotFoundException($"The draft does not contain {id}.");
            return lanes[index];
        }

        public bool TryGet(LaneId id, out LaneDraft lane) {
            var index = IndexOf(id);
            if (index < 0) {
                lane = null!;
                return false;
            }
            lane = lanes[index];
            return true;
        }

        //Mutation
        /// <summary>
        /// Inserts a lane at <paramref name="index"/>, where 0 is the leftmost position and
        /// <see cref="Count"/> is the rightmost. Every lane at or beyond the index shifts outward; the
        /// lanes before it keep their offsets.
        /// </summary>
        /// <returns>the identity of the newly inserted lane</returns>
        public LaneId Insert(int index, LaneSpec spec) {
            if (index < 0 || index > lanes.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be in [0, {lanes.Count}].");
            var id = new LaneId(nextId++);
            lanes.Insert(index, new LaneDraft(id, spec));
            return id;
        }

        /// <summary>Appends a lane at the rightmost position.</summary>
        public LaneId Add(LaneSpec spec) => Insert(lanes.Count, spec);

        /// <summary>Removes the lane with the given identity.</summary>
        /// <returns><see langword="true"/> if the lane was present</returns>
        public bool Remove(LaneId id) {
            var index = IndexOf(id);
            if (index < 0) return false;
            lanes.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Moves a lane to a new index. The index is interpreted against the list <i>after</i> the lane
        /// has been lifted out, so moving a lane one position to the right is <c>Move(id, index + 1)</c>.
        /// </summary>
        public void Move(LaneId id, int newIndex) {
            var index = IndexOf(id);
            if (index < 0) throw new KeyNotFoundException($"The draft does not contain {id}.");
            if (newIndex < 0 || newIndex >= lanes.Count)
                throw new ArgumentOutOfRangeException(nameof(newIndex), newIndex, $"Index must be in [0, {lanes.Count - 1}].");
            if (index == newIndex) return;
            var lane = lanes[index];
            lanes.RemoveAt(index);
            lanes.Insert(newIndex, lane);
        }

        /// <summary>Replaces the specification of an existing lane, keeping its identity and position.</summary>
        public void SetSpec(LaneId id, LaneSpec spec) => Get(id).Spec = spec;

        /// <summary>
        /// Flips the lane's direction by toggling <see cref="LaneFlags.IsMerge"/> through
        /// <see cref="LaneSpec.Reverse"/>. See the design document, section 5.4.
        /// </summary>
        public void ToggleDirection(LaneId id) {
            var lane = Get(id);
            lane.Spec = lane.Spec.Reverse();
        }

        /// <summary>
        /// Inserts a copy of <paramref name="id"/> on the given side <b>without moving the original</b>.
        /// <paramref name="side"/> is negative for left of the original, positive for right of it.
        /// <para>
        /// This is the "exit" operation: the original lane keeps its exact position and width, and only
        /// the lanes beyond the insertion point shift. See the design document, section 5.5.
        /// </para>
        /// </summary>
        /// <param name="overrideSpec">specification for the new lane; the original's spec when omitted</param>
        /// <returns>the identity of the new lane</returns>
        public LaneId Exit(LaneId id, int side, LaneSpec? overrideSpec = null) {
            if (side == 0) throw new ArgumentException("Side must be non-zero: negative is left, positive is right.", nameof(side));
            var index = IndexOf(id);
            if (index < 0) throw new KeyNotFoundException($"The draft does not contain {id}.");

            var spec = overrideSpec ?? lanes[index].Spec;
            var insertAt = side < 0 ? index : index + 1;

            //Offsets are derived from a cross-section centred on Centerline, so adding a lane would
            //otherwise re-centre everything and shift the original. Moving the centreline by half the new
            //lane's width in the direction the cross-section grew keeps every lane before the insertion
            //point at exactly its previous offset, which is what makes this an exit rather than an insert.
            Centerline += side * spec.Width / 2;

            return Insert(insertAt, spec);
        }

        /// <summary>
        /// Merges the lane at <paramref name="index"/> with the one to its right into a single lane whose
        /// specification is the union of the two. See the design document, section 5.2.
        /// </summary>
        /// <returns>the identity of the merged lane</returns>
        public LaneId Merge(int index) {
            if (index < 0 || index + 1 >= lanes.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"There must be a lane to the right of index {index}.");
            var merged = MergeSpecs(lanes[index].Spec, lanes[index + 1].Spec);
            var id = lanes[index].Id;
            lanes.RemoveRange(index, 2);
            lanes.Insert(index, new LaneDraft(id, merged));
            return id;
        }

        /// <summary>
        /// Splits the lane at <paramref name="index"/> into two lanes at <paramref name="fraction"/> of its
        /// width, each inheriting the parent's specification. See the design document, section 5.3.
        /// </summary>
        /// <param name="fraction">split point as a fraction of the parent's width, in (0, 1)</param>
        /// <returns>the identities of the left and right halves</returns>
        public (LaneId Left, LaneId Right) Split(int index, float fraction = 0.5f) {
            if (index < 0 || index >= lanes.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be in [0, {lanes.Count - 1}].");
            if (fraction <= 0 || fraction >= 1)
                throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Fraction must be strictly between 0 and 1.");

            var parent = lanes[index];
            var leftSpec = parent.Spec;
            var rightSpec = parent.Spec;
            leftSpec.Width = parent.Spec.Width * fraction;
            rightSpec.Width = parent.Spec.Width * (1 - fraction);

            var leftId = new LaneId(nextId++);
            var rightId = new LaneId(nextId++);
            lanes.RemoveAt(index);
            lanes.Insert(index, new LaneDraft(leftId, leftSpec));
            lanes.Insert(index + 1, new LaneDraft(rightId, rightSpec));
            return (leftId, rightId);
        }

        /// <summary>
        /// The union of two lane specifications, used by <see cref="Merge"/>. Because
        /// <see cref="VehicleTypes"/> is a flags enum, merging a car lane with a tram lane yields a lane
        /// that carries both.
        /// </summary>
        public static LaneSpec MergeSpecs(LaneSpec a, LaneSpec b) => new(
            color: a.Color,
            vehicleTypes: a.VehicleTypes | b.VehicleTypes,
            width: MathF.Max(a.Width, b.Width),
            speedLimit: MathF.Min(a.SpeedLimit, b.SpeedLimit),
            flags: a.Flags | b.Flags,
            lineWidth: a.LineWidth,
            surface: a.Surface
        );

        /// <summary>
        /// Derives the centre offset of every lane from the ordered list. Lane <c>i</c> is placed so that
        /// the lanes are contiguous and the whole cross-section is centred on <see cref="Centerline"/>.
        /// </summary>
        public float[] ComputeOffsets() {
            var result = new float[lanes.Count];
            if (lanes.Count == 0) return result;

            var widths = new float[lanes.Count];
            var total = 0f;
            for (int i = 0; i < lanes.Count; i++) {
                widths[i] = lanes[i].Spec.Width;
                total += widths[i];
            }

            //Scale the widths to fit when a total width has been forced.
            var scale = 1f;
            if (ForcedWidth.HasValue && total > 0) {
                scale = ForcedWidth.Value / total;
                total = ForcedWidth.Value;
            }

            var x = Centerline - total / 2;
            for (int i = 0; i < lanes.Count; i++) {
                var width = widths[i] * scale;
                result[i] = x + width / 2;
                x += width;
            }
            return result;
        }

        /// <summary>
        /// The cross-section extent of every lane, derived from <see cref="ComputeOffsets"/>.
        /// </summary>
        public Interval<float>[] ComputeBounds() {
            var offsets = ComputeOffsets();
            var result = new Interval<float>[lanes.Count];
            var scale = 1f;
            if (ForcedWidth.HasValue) {
                var total = lanes.Sum(x => x.Spec.Width);
                if (total > 0) scale = ForcedWidth.Value / total;
            }
            for (int i = 0; i < lanes.Count; i++) {
                var half = lanes[i].Spec.Width * scale / 2;
                result[i] = new(offsets[i] - half, offsets[i] + half);
            }
            return result;
        }

        /// <summary>
        /// Converts the draft into a <see cref="NodeSpec"/>, assigning a fresh <see cref="Guid"/> to every
        /// lane. The draft's own <see cref="LaneId"/>s are tool-side identities and are deliberately not
        /// carried into the world model.
        /// </summary>
        public NodeSpec ToNodeSpec() {
            var offsets = ComputeOffsets();
            var scale = 1f;
            if (ForcedWidth.HasValue) {
                var total = lanes.Sum(x => x.Spec.Width);
                if (total > 0) scale = ForcedWidth.Value / total;
            }

            var laneNodes = new LaneNode[lanes.Count];
            for (int i = 0; i < lanes.Count; i++) {
                var spec = lanes[i].Spec;
                spec.Width = lanes[i].Spec.Width * scale;
                laneNodes[i] = new LaneNode(spec, offsets[i]);
            }
            return new NodeSpec(laneNodes);
        }

        /// <summary>
        /// Builds a draft from an existing <see cref="NodeSpec"/>. Lanes are taken in left-to-right order
        /// and the centreline is placed at the middle of the spec's range.
        /// </summary>
        public static NodeSpecDraft FromNodeSpec(NodeSpec spec) {
            var draft = new NodeSpecDraft(spec.Range.Middle());
            foreach (var lane in spec.Lanes)
                draft.Add(lane.LaneSpec);
            return draft;
        }

        /// <summary>Builds a draft from a half-node, in that half's own left-to-right order.</summary>
        public static NodeSpecDraft FromHalfNode(HalfNode halfNode) {
            var draft = new NodeSpecDraft(halfNode.Bounds.Middle());
            for (int i = 0; i < halfNode.LaneCount; i++)
                draft.Add(halfNode.GetLaneByIndex(i).LaneSpec);
            return draft;
        }

        /// <summary>An independent copy of this draft, with the same identities.</summary>
        public NodeSpecDraft Clone() {
            var result = new NodeSpecDraft(Centerline) {
                ForcedWidth = ForcedWidth,
                nextId = nextId
            };
            foreach (var lane in lanes) result.lanes.Add(lane.Clone());
            return result;
        }

        /// <summary>Reverses the left-to-right order of the lanes, keeping their identities.</summary>
        public void Mirror() => lanes.Reverse();

        public IEnumerator<LaneDraft> GetEnumerator() => lanes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
