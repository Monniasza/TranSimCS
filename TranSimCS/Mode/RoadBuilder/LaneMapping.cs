using System;
using System.Collections.Generic;
using System.Linq;
using TranSimCS.Roads;

namespace TranSimCS.Mode.RoadBuilder {
    /// <summary>
    /// Which side of an anchor lane a destination-only lane is spliced in on. Negative is to the left of
    /// the anchor, positive is to the right.
    /// </summary>
    public enum InsertionSide {
        Left = -1,
        Right = 1
    }

    /// <summary>
    /// A pairing between a source lane and a destination lane.
    /// </summary>
    public readonly struct LanePair : IEquatable<LanePair> {
        public readonly LaneId Source;
        public readonly LaneId Dest;

        public LanePair(LaneId source, LaneId dest) {
            Source = source;
            Dest = dest;
        }

        public bool Equals(LanePair other) => Source == other.Source && Dest == other.Dest;
        public override bool Equals(object? obj) => obj is LanePair other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Source, Dest);
        public override string ToString() => $"({Source} -> {Dest})";

        public static bool operator ==(LanePair left, LanePair right) => left.Equals(right);
        public static bool operator !=(LanePair left, LanePair right) => !left.Equals(right);
    }

    /// <summary>
    /// Where a destination-only lane is spliced into the source ordering.
    /// </summary>
    public readonly struct LaneInsertion : IEquatable<LaneInsertion> {
        /// <summary>The source lane the new lane is placed next to.</summary>
        public readonly LaneId Anchor;
        /// <summary>Which side of the anchor the new lane goes.</summary>
        public readonly InsertionSide Side;
        /// <summary>The destination lane that is being spliced in.</summary>
        public readonly LaneId Inserted;

        public LaneInsertion(LaneId anchor, InsertionSide side, LaneId inserted) {
            Anchor = anchor;
            Side = side;
            Inserted = inserted;
        }

        public bool Equals(LaneInsertion other)
            => Anchor == other.Anchor && Side == other.Side && Inserted == other.Inserted;
        public override bool Equals(object? obj) => obj is LaneInsertion other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Anchor, Side, Inserted);
        public override string ToString() => $"({Inserted} {Side} of {Anchor})";

        public static bool operator ==(LaneInsertion left, LaneInsertion right) => left.Equals(right);
        public static bool operator !=(LaneInsertion left, LaneInsertion right) => !left.Equals(right);
    }

    /// <summary>
    /// A mapping between the lanes of a source node and the lanes of a destination node.
    /// <para>
    /// This replaces the positional <c>LaneMappingInputs</c> / <c>LaneMappingOutput</c> pair. Those
    /// described a mapping as <c>(source index, destination index)</c> pairs, which cannot express
    /// "insert a lane between index 2 and 3" because inserting shifts every index after it and so
    /// invalidates the mapping by its own application. Here lanes are referred to by
    /// <see cref="LaneId"/>, so an insertion never invalidates anything.
    /// </para>
    /// <para>
    /// A mapping is <b>derived</b> from two drafts rather than authored, but it is editable: the user can
    /// re-link lanes, and the result is shown as coloured connectors. It is never the source of truth for
    /// the geometry.
    /// </para>
    /// <para>
    /// <b>How merges and exits are expressed.</b> A mapping is a set of lane strips, each of which
    /// connects one source lane end to one destination lane end. Two shapes of that set are named:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// A <b>merge</b> is a collection of lane strips with a <i>shared end lane</i> and <i>disjoint start
    /// lanes</i>. Several source lanes converge onto one destination lane, so the destination lane is
    /// paired with exactly one of them and the rest become <see cref="SourceOnly"/>: they terminate at
    /// the node. A merge therefore shows up as one <see cref="Matched"/> pair plus one or more
    /// <see cref="SourceOnly"/> lanes, and never as a <see cref="DestOnly"/> lane.
    /// </item>
    /// <item>
    /// An <b>exit</b> is a collection of lane strips with a <i>shared start lane</i> and <i>disjoint end
    /// lanes</i>. One source lane diverges into several destination lanes, so the source lane is paired
    /// with exactly one of them and the rest become <see cref="DestOnly"/>: they begin at the node. An
    /// exit therefore shows up as one <see cref="Matched"/> pair plus one or more
    /// <see cref="DestOnly"/> lanes, each of which needs an <see cref="Insertions"/> entry, and never as
    /// a <see cref="SourceOnly"/> lane.
    /// </item>
    /// </list>
    /// <para>
    /// The two are not mutually exclusive: a node can merge on one side of the cross-section and exit on
    /// the other, which is simply a mapping that has both <see cref="SourceOnly"/> and
    /// <see cref="DestOnly"/> lanes. Because a lane is identified by <see cref="LaneId"/> rather than by
    /// index, a merge and an exit can be added, removed or re-pointed independently without disturbing
    /// each other.
    /// </para>
    /// </summary>
    public sealed class LaneMapping {
        private readonly List<LanePair> matched = [];
        private readonly List<LaneId> sourceOnly = [];
        private readonly List<LaneId> destOnly = [];
        private readonly List<LaneInsertion> insertions = [];

        /// <summary>
        /// Lanes that exist on both sides, paired by identity. Each pair is one lane strip: it connects
        /// the source lane end to the destination lane end.
        /// <para>
        /// A merge contributes exactly one pair (the destination lane it converges onto) and an exit
        /// contributes exactly one pair (the source lane it diverges from); the other strips of the merge
        /// or exit are recorded in <see cref="SourceOnly"/> and <see cref="DestOnly"/> respectively.
        /// </para>
        /// </summary>
        public IReadOnlyList<LanePair> Matched => matched;

        /// <summary>
        /// Lanes present only on the source; they terminate at the node and have no destination
        /// counterpart, so they do not appear in <see cref="BuildDestinationOrder"/>.
        /// <para>
        /// These are the <i>converging</i> strips of a <b>merge</b>: a merge is a collection of lane
        /// strips with a shared end lane and disjoint start lanes, so all but one of its start lanes are
        /// listed here. A lane appears here only if it is not in <see cref="Matched"/>.
        /// </para>
        /// </summary>
        public IReadOnlyList<LaneId> SourceOnly => sourceOnly;

        /// <summary>
        /// Lanes present only on the destination; they begin at the node and have no source counterpart.
        /// <para>
        /// These are the <i>diverging</i> strips of an <b>exit</b>: an exit is a collection of lane
        /// strips with a shared start lane and disjoint end lanes, so all but one of its end lanes are
        /// listed here. Every lane in this list must have a matching entry in
        /// <see cref="Insertions"/>, which is what tells the destination cross-section where to place it;
        /// <see cref="Validate"/> enforces this. A lane appears here only if it is not in
        /// <see cref="Matched"/>.
        /// </para>
        /// </summary>
        public IReadOnlyList<LaneId> DestOnly => destOnly;

        /// <summary>
        /// Where each destination-only lane is spliced into the source ordering. There is exactly one
        /// entry per <see cref="DestOnly"/> lane once the mapping is valid.
        /// <para>
        /// This is what makes an <b>exit</b> placeable: the diverging lanes of an exit are all anchored
        /// on the shared start lane, so they are spliced in beside it rather than appended at the edge.
        /// </para>
        /// </summary>
        public IReadOnlyList<LaneInsertion> Insertions => insertions;

        /// <summary>
        /// True when every source lane is paired and every destination lane is paired, so the mapping
        /// contains neither a merge nor an exit.
        /// </summary>
        public bool IsComplete => sourceOnly.Count == 0 && destOnly.Count == 0;

        /// <summary>
        /// True when the two sides have nothing in common. Every source lane is then in
        /// <see cref="SourceOnly"/> and every destination lane in <see cref="DestOnly"/>.
        /// </summary>
        public bool IsDisjoint => matched.Count == 0;

        //Construction
        /// <summary>
        /// Derives a mapping between two drafts by longest common subsequence on <see cref="LaneSpec"/>
        /// equality. See <see cref="LaneMappingDeriver"/>.
        /// </summary>
        public static LaneMapping Derive(NodeSpecDraft source, NodeSpecDraft dest)
            => LaneMappingDeriver.Derive(source, dest);

        /// <summary>
        /// Derives a mapping between two drafts, treating lanes whose specs match under
        /// <paramref name="specComparer"/> as equal. See <see cref="LaneMappingDeriver"/>.
        /// </summary>
        public static LaneMapping Derive(NodeSpecDraft source, NodeSpecDraft dest, Func<LaneSpec, LaneSpec, bool> specComparer)
            => LaneMappingDeriver.Derive(source, dest, specComparer);

        /// <summary>An empty mapping, used as the starting point for a hand-authored one.</summary>
        public LaneMapping() { }

        //Editing
        /// <summary>
        /// Pairs a source lane with a destination lane, adding one lane strip to the mapping. Both lanes
        /// are removed from <see cref="SourceOnly"/> and <see cref="DestOnly"/> if they were there.
        /// <para>
        /// This is how a merge or an exit is built up: pair the shared lane first, then mark the other
        /// strips with <see cref="AddSourceOnly"/> (merge) or <see cref="AddDestOnly"/> (exit).
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The source lane is already in <see cref="Matched"/>, or the destination lane is.
        /// </exception>
        public void AddMatch(LaneId source, LaneId dest) {
            if (matched.Any(x => x.Source == source))
                throw new InvalidOperationException($"{source} is already matched.");
            if (matched.Any(x => x.Dest == dest))
                throw new InvalidOperationException($"{dest} is already matched.");
            sourceOnly.Remove(source);
            destOnly.Remove(dest);
            matched.Add(new LanePair(source, dest));
        }

        /// <summary>
        /// Marks a source lane as terminating at the node, adding one converging lane strip to the
        /// mapping. The lane has no destination counterpart and will not appear in
        /// <see cref="BuildDestinationOrder"/>.
        /// <para>
        /// This is the second half of a <b>merge</b>: after <see cref="AddMatch"/> pairs the destination
        /// lane the merge converges onto, call this for each of the other start lanes of the merge.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The lane is already in <see cref="Matched"/>, so it cannot also be source-only.
        /// </exception>
        public void AddSourceOnly(LaneId source) {
            if (matched.Any(x => x.Source == source))
                throw new InvalidOperationException($"{source} is already matched.");
            if (!sourceOnly.Contains(source)) sourceOnly.Add(source);
        }

        /// <summary>
        /// Marks a destination lane as beginning at the node, adding one diverging lane strip to the
        /// mapping. The lane has no source counterpart, so it must also be given an insertion point with
        /// <see cref="AddInsertion"/> before the mapping will <see cref="Validate"/>.
        /// <para>
        /// This is the second half of an <b>exit</b>: after <see cref="AddMatch"/> pairs the source lane
        /// the exit diverges from, call this for each of the other end lanes of the exit.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The lane is already in <see cref="Matched"/>, so it cannot also be destination-only.
        /// </exception>
        public void AddDestOnly(LaneId dest) {
            if (matched.Any(x => x.Dest == dest))
                throw new InvalidOperationException($"{dest} is already matched.");
            if (!destOnly.Contains(dest)) destOnly.Add(dest);
        }

        /// <summary>
        /// Records where a destination-only lane is spliced into the source ordering, relative to an
        /// anchor lane. Replaces any insertion already recorded for the same lane.
        /// <para>
        /// For an <b>exit</b>, every diverging lane is anchored on the shared start lane, so the whole
        /// exit is spliced in beside that one lane rather than appended at the edge of the cross-section.
        /// </para>
        /// </summary>
        /// <param name="anchor">
        /// The source lane the new lane is placed next to. It must be in <see cref="Matched"/>, because
        /// the splice position is resolved through its destination partner.
        /// </param>
        /// <param name="side">Which side of the anchor the new lane goes.</param>
        /// <param name="inserted">The destination-only lane being placed.</param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="inserted"/> is not in <see cref="DestOnly"/>.
        /// </exception>
        public void AddInsertion(LaneId anchor, InsertionSide side, LaneId inserted) {
            if (!destOnly.Contains(inserted))
                throw new InvalidOperationException($"{inserted} is not a destination-only lane.");
            insertions.RemoveAll(x => x.Inserted == inserted);
            insertions.Add(new LaneInsertion(anchor, side, inserted));
        }

        /// <summary>
        /// Removes a pairing, returning both lanes to their respective only-lists: the source lane to
        /// <see cref="SourceOnly"/> and the destination lane to <see cref="DestOnly"/>.
        /// <para>
        /// The destination lane is left without an insertion point, so the mapping will not
        /// <see cref="Validate"/> until one is added with <see cref="AddInsertion"/>.
        /// </para>
        /// </summary>
        /// <returns><see langword="true"/> if the source lane was paired</returns>
        public bool RemoveMatch(LaneId source) {
            var index = matched.FindIndex(x => x.Source == source);
            if (index < 0) return false;
            var pair = matched[index];
            matched.RemoveAt(index);
            sourceOnly.Add(pair.Source);
            destOnly.Add(pair.Dest);
            return true;
        }

        //Queries
        /// <summary>
        /// The destination lane paired with a source lane, if any.
        /// <para>
        /// Returns <see langword="null"/> for a lane in <see cref="SourceOnly"/>, which is the
        /// converging side of a <b>merge</b>: such a lane terminates at the node and has no destination
        /// counterpart.
        /// </para>
        /// </summary>
        public LaneId? DestFor(LaneId source) {
            foreach (var pair in matched)
                if (pair.Source == source) return pair.Dest;
            return null;
        }

        /// <summary>
        /// The source lane paired with a destination lane, if any.
        /// <para>
        /// Returns <see langword="null"/> for a lane in <see cref="DestOnly"/>, which is the diverging
        /// side of an <b>exit</b>: such a lane begins at the node and has no source counterpart.
        /// </para>
        /// </summary>
        public LaneId? SourceFor(LaneId dest) {
            foreach (var pair in matched)
                if (pair.Dest == dest) return pair.Source;
            return null;
        }

        /// <summary>
        /// The insertion recorded for a destination-only lane, if any.
        /// <para>
        /// Returns <see langword="null"/> for a lane that is not in <see cref="DestOnly"/>, and also for
        /// a destination-only lane that has not yet been given an insertion point - a state
        /// <see cref="Validate"/> rejects.
        /// </para>
        /// </summary>
        public LaneInsertion? InsertionFor(LaneId inserted) {
            foreach (var insertion in insertions)
                if (insertion.Inserted == inserted) return insertion;
            return null;
        }

        /// <summary>
        /// The destination lanes in left-to-right order, with destination-only lanes spliced in at their
        /// recorded insertion points. This is the ordering the destination cross-section is built from.
        /// <para>
        /// Source-only lanes do not appear: they terminate at the node and have no destination
        /// counterpart. This is what makes a <b>merge</b> collapse correctly - the converging lanes are
        /// simply absent from the result, leaving the single lane they merged into. Insertion anchors are
        /// source lanes, so they are resolved to their destination partners before the splice position is
        /// looked up.
        /// </para>
        /// <para>
        /// Every <see cref="DestOnly"/> lane is placed beside its anchor, which is what makes an
        /// <b>exit</b> land in the right part of the cross-section rather than at the edge. A
        /// destination-only lane whose anchor is not in <see cref="Matched"/> is skipped, since its
        /// position cannot be resolved; <see cref="Validate"/> does not reject that case, so callers that
        /// need every lane placed should check the result against <see cref="DestOnly"/>.
        /// </para>
        /// </summary>
        public IReadOnlyList<LaneId> BuildDestinationOrder() {
            var order = new List<LaneId>();
            foreach (var pair in matched) order.Add(pair.Dest);

            //Splice each destination-only lane in next to its anchor. Insertions are applied left to
            //right so that several lanes anchored to the same lane keep their relative order.
            foreach (var insertion in insertions.OrderBy(x => x.Anchor.Value)) {
                //The anchor is a source lane; find where its destination partner sits in the order.
                var anchorDest = DestFor(insertion.Anchor);
                var anchorIndex = anchorDest is LaneId dest ? order.IndexOf(dest) : -1;
                if (anchorIndex < 0) continue;
                var at = insertion.Side == InsertionSide.Left ? anchorIndex : anchorIndex + 1;
                order.Insert(at, insertion.Inserted);
            }
            return order;
        }

        /// <summary>
        /// Validates that the mapping is internally consistent: every lane appears exactly once across
        /// the three lists, and every destination-only lane has an insertion point.
        /// <para>
        /// The insertion-point rule is what keeps an <b>exit</b> well formed: a diverging lane that is
        /// not anchored anywhere could not be placed in the destination cross-section. A <b>merge</b>
        /// needs no equivalent rule, because a converging lane is simply absent from the destination and
        /// so has nothing to place.
        /// </para>
        /// <para>
        /// This checks membership only, not provenance: <see cref="LaneId"/> values are unique within a
        /// draft but not across drafts, so a lane from a different draft that happens to share an id
        /// cannot be detected.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">The mapping is inconsistent.</exception>
        public void Validate(NodeSpecDraft source, NodeSpecDraft dest) {
            var seenSource = new HashSet<LaneId>();
            var seenDest = new HashSet<LaneId>();

            foreach (var pair in matched) {
                if (!source.Contains(pair.Source))
                    throw new InvalidOperationException($"Matched source lane {pair.Source} is not in the source draft.");
                if (!dest.Contains(pair.Dest))
                    throw new InvalidOperationException($"Matched destination lane {pair.Dest} is not in the destination draft.");
                if (!seenSource.Add(pair.Source))
                    throw new InvalidOperationException($"Source lane {pair.Source} appears more than once.");
                if (!seenDest.Add(pair.Dest))
                    throw new InvalidOperationException($"Destination lane {pair.Dest} appears more than once.");
            }

            foreach (var lane in sourceOnly) {
                if (!source.Contains(lane))
                    throw new InvalidOperationException($"Source-only lane {lane} is not in the source draft.");
                if (!seenSource.Add(lane))
                    throw new InvalidOperationException($"Source lane {lane} appears more than once.");
            }

            foreach (var lane in destOnly) {
                if (!dest.Contains(lane))
                    throw new InvalidOperationException($"Destination-only lane {lane} is not in the destination draft.");
                if (!seenDest.Add(lane))
                    throw new InvalidOperationException($"Destination lane {lane} appears more than once.");
                if (InsertionFor(lane) is null)
                    throw new InvalidOperationException($"Destination-only lane {lane} has no insertion point.");
            }

            //Every lane on each side must be accounted for.
            foreach (var lane in source)
                if (!seenSource.Contains(lane.Id))
                    throw new InvalidOperationException($"Source lane {lane.Id} is not accounted for by the mapping.");
            foreach (var lane in dest)
                if (!seenDest.Contains(lane.Id))
                    throw new InvalidOperationException($"Destination lane {lane.Id} is not accounted for by the mapping.");
        }

        /// <summary>
        /// A summary of the mapping. The source-only count is the number of converging strips beyond the
        /// first of each <b>merge</b>, and the destination-only count is the number of diverging strips
        /// beyond the first of each <b>exit</b>.
        /// </summary>
        public override string ToString()
            => $"LaneMapping(matched: {matched.Count}, source-only: {sourceOnly.Count}, dest-only: {destOnly.Count})";
    }
}
