using System;
using System.Collections.Generic;
using TranSimCS.Roads;

namespace TranSimCS.Mode.RoadBuilder {
    /// <summary>
    /// Derives a <see cref="LaneMapping"/> between two drafts by longest common subsequence.
    /// <para>
    /// LCS is the standard dynamic-programming algorithm for finding the longest sequence of elements
    /// that appears in <i>both</i> input sequences in the same relative order, without requiring the
    /// elements to be contiguous. Applied to two lane lists, the two sequences are the source lanes and
    /// the destination lanes, each in left-to-right order, and two lanes match when their
    /// <see cref="LaneSpec"/>s are equal.
    /// </para>
    /// <para>
    /// Order preservation is the property that makes LCS the right tool here: it will never pair the
    /// leftmost source lane with the rightmost destination lane, so the resulting mapping is always
    /// geometrically sane and never produces crossing connectors.
    /// </para>
    /// <para>
    /// The DP table is <c>O(n·m)</c> in time and space for <c>n</c> source and <c>m</c> destination
    /// lanes, which is trivial at road widths (a handful of lanes), so no optimisation is needed.
    /// </para>
    /// <para>
    /// <b>Merges and exits.</b> The LCS pairs each lane with at most one lane on the other side, so a
    /// many-to-one or one-to-many shape is expressed by leaving the surplus lanes unpaired:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// A <b>merge</b> - several source lanes converging onto one destination lane - is derived as one
    /// <see cref="LaneMapping.Matched"/> pair plus a <see cref="LaneMapping.SourceOnly"/> entry for each
    /// of the other converging lanes. Those lanes terminate at the node.
    /// </item>
    /// <item>
    /// An <b>exit</b> - one source lane diverging into several destination lanes - is derived as one
    /// <see cref="LaneMapping.Matched"/> pair plus a <see cref="LaneMapping.DestOnly"/> entry for each of
    /// the other diverging lanes, each with an insertion point anchored on the shared source lane.
    /// </item>
    /// </list>
    /// <para>
    /// Which lane of a merge or exit ends up paired is decided by the LCS, not by any explicit rule: it
    /// is whichever choice keeps the pairing longest and in order. The remaining lanes are then
    /// classified by which side they are on.
    /// </para>
    /// <para>
    /// <b>Disjoint specs.</b> When the two drafts have no lane in common the LCS is empty, so there is no
    /// matched lane for a destination-only lane to anchor on. The derived mapping is then deliberately
    /// left invalid - <see cref="LaneMapping.Validate"/> rejects it - because a mapping with no matched
    /// lane has no geometry to build from. The user resolves this by linking at least one lane by hand
    /// with <see cref="LaneMapping.AddMatch"/>, after which the rest can be re-derived or linked too.
    /// </para>
    /// </summary>
    public static class LaneMappingDeriver {
        /// <summary>
        /// Derives a mapping between two drafts, matching lanes whose <see cref="LaneSpec"/>s are equal.
        /// <para>
        /// Lanes that match on neither side become <see cref="LaneMapping.SourceOnly"/> (the converging
        /// strips of a merge) or <see cref="LaneMapping.DestOnly"/> (the diverging strips of an exit).
        /// </para>
        /// </summary>
        public static LaneMapping Derive(NodeSpecDraft source, NodeSpecDraft dest)
            => Derive(source, dest, static (a, b) => a.Equals(b));

        /// <summary>
        /// Derives a mapping between two drafts, treating lanes whose specs match under
        /// <paramref name="specComparer"/> as equal.
        /// <para>
        /// The comparer decides what counts as "the same lane" and therefore which lanes are left over as
        /// <see cref="LaneMapping.SourceOnly"/> or <see cref="LaneMapping.DestOnly"/>. A looser comparer
        /// (for example one that ignores width) pairs more lanes and so derives fewer merges and exits.
        /// </para>
        /// </summary>
        public static LaneMapping Derive(NodeSpecDraft source, NodeSpecDraft dest, Func<LaneSpec, LaneSpec, bool> specComparer) {
            ArgumentNullException.ThrowIfNull(source, nameof(source));
            ArgumentNullException.ThrowIfNull(dest, nameof(dest));
            ArgumentNullException.ThrowIfNull(specComparer, nameof(specComparer));

            var n = source.Count;
            var m = dest.Count;

            //Build the LCS length table. lengths[i, j] is the LCS length of source[i..] and dest[j..].
            var lengths = new int[n + 1, m + 1];
            for (int i = n - 1; i >= 0; i--) {
                for (int j = m - 1; j >= 0; j--) {
                    if (specComparer(source[i].Spec, dest[j].Spec))
                        lengths[i, j] = lengths[i + 1, j + 1] + 1;
                    else
                        lengths[i, j] = Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
                }
            }

            //Walk the table to recover the pairing, in left-to-right order on both sides.
            var pairs = new List<LanePair>();
            var matchedSource = new bool[n];
            var matchedDest = new bool[m];
            for (int i = 0, j = 0; i < n && j < m;) {
                if (specComparer(source[i].Spec, dest[j].Spec)) {
                    pairs.Add(new LanePair(source[i].Id, dest[j].Id));
                    matchedSource[i] = true;
                    matchedDest[j] = true;
                    i++;
                    j++;
                } else if (lengths[i + 1, j] >= lengths[i, j + 1]) {
                    i++;
                } else {
                    j++;
                }
            }

            var mapping = new LaneMapping();
            foreach (var pair in pairs) mapping.AddMatch(pair.Source, pair.Dest);

            //Source lanes not in the LCS terminate at the node.
            for (int i = 0; i < n; i++)
                if (!matchedSource[i]) mapping.AddSourceOnly(source[i].Id);

            //Destination lanes not in the LCS begin at the node, and are spliced in next to their
            //nearest LCS neighbour.
            for (int j = 0; j < m; j++) {
                if (matchedDest[j]) continue;
                var lane = dest[j].Id;
                mapping.AddDestOnly(lane);
                var (anchor, side) = FindInsertionPoint(source, dest, matchedSource, matchedDest, j);
                if (anchor is LaneId anchorId) mapping.AddInsertion(anchorId, side, lane);
            }

            return mapping;
        }

        /// <summary>
        /// Finds where a destination-only lane at index <paramref name="destIndex"/> should be spliced
        /// into the source ordering: next to the nearest matched lane, on the side it sits on.
        /// <para>
        /// This is what places the diverging lanes of an <b>exit</b>. When several destination lanes
        /// diverge from one source lane, they all resolve to that same source lane as their anchor, so
        /// they are spliced in beside it rather than appended at the edge of the cross-section.
        /// </para>
        /// <para>
        /// Returns a <see langword="null"/> anchor only when the source draft is empty, in which case
        /// there is no lane to anchor on and the caller records no insertion.
        /// </para>
        /// </summary>
        private static (LaneId? Anchor, InsertionSide Side) FindInsertionPoint(
            NodeSpecDraft source, NodeSpecDraft dest,
            bool[] matchedSource, bool[] matchedDest, int destIndex) {

            //Look left and right for the nearest matched destination lane, and use its source partner
            //as the anchor. The nearer neighbour wins; ties go to the left so that a run of new lanes
            //keeps its order.
            for (int distance = 1; distance < dest.Count; distance++) {
                var left = destIndex - distance;
                if (left >= 0 && matchedDest[left]) {
                    var anchor = FindSourcePartner(source, dest, matchedSource, matchedDest, left);
                    if (anchor is LaneId anchorId) return (anchorId, InsertionSide.Right);
                }

                var right = destIndex + distance;
                if (right < dest.Count && matchedDest[right]) {
                    var anchor = FindSourcePartner(source, dest, matchedSource, matchedDest, right);
                    if (anchor is LaneId anchorId) return (anchorId, InsertionSide.Left);
                }
            }

            //Nothing matched at all. There is no matched lane to anchor on, so the lane cannot be placed
            //relative to the source cross-section; the caller records no insertion and Validate rejects
            //the mapping. This is the correct outcome: a mapping with no matched lane has no geometry to
            //build from, and the user has to link at least one lane by hand.
            return (null, InsertionSide.Right);
        }

        /// <summary>
        /// Finds the source lane that the matched destination lane at <paramref name="destIndex"/> is
        /// paired with. The pairing is monotonic, so the n-th matched destination lane corresponds to
        /// the n-th matched source lane.
        /// <para>
        /// This is the step that turns a matched destination lane back into the source lane an
        /// <b>exit</b> diverges from, which is then used as the anchor for the exit's other lanes.
        /// </para>
        /// </summary>
        /// <returns>
        /// The paired source lane, or <see langword="null"/> if the destination lane is not matched.
        /// </returns>
        private static LaneId? FindSourcePartner(
            NodeSpecDraft source, NodeSpecDraft dest,
            bool[] matchedSource, bool[] matchedDest, int destIndex) {

            var destMatched = new List<int>();
            for (int j = 0; j < dest.Count; j++)
                if (matchedDest[j]) destMatched.Add(j);

            var sourceMatched = new List<int>();
            for (int i = 0; i < source.Count; i++)
                if (matchedSource[i]) sourceMatched.Add(i);

            var position = destMatched.IndexOf(destIndex);
            if (position < 0 || position >= sourceMatched.Count) return null;
            return source[sourceMatched[position]].Id;
        }
    }
}
