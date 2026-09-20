using TranSimCS;
using TranSimCS.Mode.RoadBuilder;
using TranSimCS.Roads;

namespace TranSimCSTests {
    /// <summary>
    /// Covers <see cref="LaneMapping"/> and <see cref="LaneMappingDeriver"/>: the longest-common-subsequence
    /// derivation of a mapping between two node specs.
    /// </summary>
    public class TestLaneMapping {
        private static LaneSpec Car(float width = 3f) => new(Colors.Gray, VehicleTypes.Car, width, 50);
        private static LaneSpec Bus(float width = 3f) => new(Colors.Red, VehicleTypes.Bus, width, 80);
        private static LaneSpec Tram(float width = 3.5f) => new(Colors.Orange, VehicleTypes.LRT, width, 80);
        private static LaneSpec Bike(float width = 2f) => new(Colors.Green, VehicleTypes.Bicycle, width, 30);
        private static LaneSpec Foot(float width = 1.5f) => new(Colors.LightGray, VehicleTypes.Pedestrian, width, 16);

        /// <summary>Builds a draft from a list of specs, returning the draft and its lane ids in order.</summary>
        private static (NodeSpecDraft Draft, LaneId[] Ids) Make(params LaneSpec[] specs) {
            var draft = new NodeSpecDraft();
            var ids = new LaneId[specs.Length];
            for (int i = 0; i < specs.Length; i++) ids[i] = draft.Add(specs[i]);
            return (draft, ids);
        }

        // --- Identical specs -----------------------------------------------------------------------

        [Fact]
        public void IdenticalSpecsMatchEveryLane() {
            var (source, s) = Make(Car(), Bus(), Tram());
            var (dest, d) = Make(Car(), Bus(), Tram());

            var mapping = LaneMapping.Derive(source, dest);

            Assert.True(mapping.IsComplete);
            Assert.Equal(3, mapping.Matched.Count);
            Assert.Empty(mapping.SourceOnly);
            Assert.Empty(mapping.DestOnly);
            Assert.Empty(mapping.Insertions);

            //The pairing is in left-to-right order on both sides.
            Assert.Equal(s[0], mapping.Matched[0].Source);
            Assert.Equal(d[0], mapping.Matched[0].Dest);
            Assert.Equal(s[1], mapping.Matched[1].Source);
            Assert.Equal(d[1], mapping.Matched[1].Dest);
            Assert.Equal(s[2], mapping.Matched[2].Source);
            Assert.Equal(d[2], mapping.Matched[2].Dest);
        }

        [Fact]
        public void IdenticalSpecsOfDifferentDraftsAreNotConfusedByIdentity() {
            var (source, s) = Make(Car(), Bus());
            var (dest, d) = Make(Car(), Bus());

            var mapping = LaneMapping.Derive(source, dest);

            //The two drafts issue their own LaneIds, so a match must pair across drafts, not within one.
            foreach (var pair in mapping.Matched) {
                Assert.Contains(pair.Source, s);
                Assert.Contains(pair.Dest, d);
            }
        }

        [Fact]
        public void EmptyDraftsProduceAnEmptyMapping() {
            var source = new NodeSpecDraft();
            var dest = new NodeSpecDraft();

            var mapping = LaneMapping.Derive(source, dest);

            Assert.True(mapping.IsComplete);
            Assert.Empty(mapping.Matched);
            Assert.Empty(mapping.SourceOnly);
            Assert.Empty(mapping.DestOnly);
        }

        [Fact]
        public void EmptySourceMakesEveryDestinationLaneDestOnly() {
            var source = new NodeSpecDraft();
            var (dest, d) = Make(Car(), Bus());

            var mapping = LaneMapping.Derive(source, dest);

            Assert.Empty(mapping.Matched);
            Assert.Empty(mapping.SourceOnly);
            Assert.Equal(new[] { d[0], d[1] }, mapping.DestOnly);
        }

        [Fact]
        public void EmptyDestinationMakesEverySourceLaneSourceOnly() {
            var (source, s) = Make(Car(), Bus());
            var dest = new NodeSpecDraft();

            var mapping = LaneMapping.Derive(source, dest);

            Assert.Empty(mapping.Matched);
            Assert.Empty(mapping.DestOnly);
            Assert.Equal(new[] { s[0], s[1] }, mapping.SourceOnly);
        }

        // --- Disjoint specs ------------------------------------------------------------------------

        [Fact]
        public void DisjointSpecsMatchNothing() {
            var (source, s) = Make(Car(), Bus());
            var (dest, d) = Make(Tram(), Bike());

            var mapping = LaneMapping.Derive(source, dest);

            Assert.True(mapping.IsDisjoint);
            Assert.Empty(mapping.Matched);
            Assert.Equal(new[] { s[0], s[1] }, mapping.SourceOnly);
            Assert.Equal(new[] { d[0], d[1] }, mapping.DestOnly);
        }

        [Fact]
        public void DisjointSpecsStillGiveEveryDestLaneAnInsertionPoint() {
            var (source, s) = Make(Car(), Bus());
            var (dest, d) = Make(Tram(), Bike());

            var mapping = LaneMapping.Derive(source, dest);

            //Every destination-only lane must have a home, even when nothing matched.
            foreach (var lane in mapping.DestOnly)
                Assert.NotNull(mapping.InsertionFor(lane));
        }

        [Fact]
        public void DisjointSpecsValidate() {
            var (source, _) = Make(Car(), Bus());
            var (dest, _) = Make(Tram(), Bike());

            var mapping = LaneMapping.Derive(source, dest);

            mapping.Validate(source, dest);
        }

        // --- Partial overlap -----------------------------------------------------------------------

        [Fact]
        public void PartialOverlapMatchesTheCommonSubsequence() {
            //The worked example from the design document, section 2.2.
            var (source, s) = Make(Car(), Bus(), Tram(), Bike());
            var (dest, d) = Make(Car(), Tram(), Bike(), Foot());

            var mapping = LaneMapping.Derive(source, dest);

            Assert.Equal(3, mapping.Matched.Count);
            Assert.Equal(s[0], mapping.Matched[0].Source);
            Assert.Equal(d[0], mapping.Matched[0].Dest);
            Assert.Equal(s[2], mapping.Matched[1].Source);
            Assert.Equal(d[1], mapping.Matched[1].Dest);
            Assert.Equal(s[3], mapping.Matched[2].Source);
            Assert.Equal(d[2], mapping.Matched[2].Dest);

            Assert.Equal(new[] { s[1] }, mapping.SourceOnly);
            Assert.Equal(new[] { d[3] }, mapping.DestOnly);
        }

        [Fact]
        public void PartialOverlapSplicesTheNewLaneNextToItsNeighbour() {
            var (source, s) = Make(Car(), Bus(), Tram(), Bike());
            var (dest, d) = Make(Car(), Tram(), Bike(), Foot());

            var mapping = LaneMapping.Derive(source, dest);

            //The footpath sits to the right of the bike lane, which is its nearest matched neighbour.
            var insertion = mapping.InsertionFor(d[3]);
            Assert.NotNull(insertion);
            Assert.Equal(s[3], insertion!.Value.Anchor);
            Assert.Equal(InsertionSide.Right, insertion.Value.Side);
        }

        [Fact]
        public void PartialOverlapValidates() {
            var (source, _) = Make(Car(), Bus(), Tram(), Bike());
            var (dest, _) = Make(Car(), Tram(), Bike(), Foot());

            var mapping = LaneMapping.Derive(source, dest);

            mapping.Validate(source, dest);
        }

        [Fact]
        public void InsertionAtTheLeftEndAnchorsOnTheLeftmostMatch() {
            var (source, s) = Make(Car(), Bus());
            var (dest, d) = Make(Foot(), Car(), Bus());

            var mapping = LaneMapping.Derive(source, dest);

            Assert.Equal(2, mapping.Matched.Count);
            Assert.Equal(new[] { d[0] }, mapping.DestOnly);

            var insertion = mapping.InsertionFor(d[0]);
            Assert.NotNull(insertion);
            Assert.Equal(s[0], insertion!.Value.Anchor);
            Assert.Equal(InsertionSide.Left, insertion.Value.Side);
        }

        [Fact]
        public void InsertionInTheMiddleAnchorsOnTheNearerNeighbour() {
            var (source, s) = Make(Car(), Tram());
            var (dest, d) = Make(Car(), Bus(), Tram());

            var mapping = LaneMapping.Derive(source, dest);

            Assert.Equal(2, mapping.Matched.Count);
            Assert.Equal(new[] { d[1] }, mapping.DestOnly);

            //The bus sits between the car and the tram; the nearer neighbour wins.
            var insertion = mapping.InsertionFor(d[1]);
            Assert.NotNull(insertion);
            Assert.Contains(insertion!.Value.Anchor, new[] { s[0], s[1] });
        }

        [Fact]
        public void SourceOnlyLanesAreThoseNotInTheSubsequence() {
            var (source, s) = Make(Car(), Bus(), Tram());
            var (dest, _) = Make(Car(), Tram());

            var mapping = LaneMapping.Derive(source, dest);

            Assert.Equal(2, mapping.Matched.Count);
            Assert.Equal(new[] { s[1] }, mapping.SourceOnly);
            Assert.Empty(mapping.DestOnly);
        }

        // --- Order preservation --------------------------------------------------------------------

        [Fact]
        public void PairingNeverCrosses() {
            var (source, s) = Make(Car(), Bus(), Tram(), Bike());
            var (dest, d) = Make(Bike(), Car(), Tram(), Bus());

            var mapping = LaneMapping.Derive(source, dest);

            //Whatever the LCS picks, the pairing must be monotonic on both sides: this is the property
            //that guarantees no crossing connectors.
            for (int i = 0; i + 1 < mapping.Matched.Count; i++) {
                var a = mapping.Matched[i];
                var b = mapping.Matched[i + 1];
                Assert.True(source.IndexOf(a.Source) < source.IndexOf(b.Source),
                    "Source indices must increase.");
                Assert.True(dest.IndexOf(a.Dest) < dest.IndexOf(b.Dest),
                    "Destination indices must increase.");
            }
        }

        [Fact]
        public void ReversedSpecsDoNotMatchPositionally() {
            var (source, s) = Make(Car(), Bus());
            var (dest, d) = Make(Bus(), Car());

            var mapping = LaneMapping.Derive(source, dest);

            //LCS preserves order, so it cannot pair car->car and bus->bus across a reversal; it picks
            //one of them and leaves the other on each side.
            Assert.Equal(1, mapping.Matched.Count);
            Assert.Equal(1, mapping.SourceOnly.Count);
            Assert.Equal(1, mapping.DestOnly.Count);
        }

        [Fact]
        public void DuplicateSpecsMatchInOrder() {
            var (source, s) = Make(Car(), Car(), Car());
            var (dest, d) = Make(Car(), Car());

            var mapping = LaneMapping.Derive(source, dest);

            //Two of the three source lanes match, and they match the first two in order.
            Assert.Equal(2, mapping.Matched.Count);
            Assert.Equal(s[0], mapping.Matched[0].Source);
            Assert.Equal(d[0], mapping.Matched[0].Dest);
            Assert.Equal(s[1], mapping.Matched[1].Source);
            Assert.Equal(d[1], mapping.Matched[1].Dest);
            Assert.Equal(new[] { s[2] }, mapping.SourceOnly);
        }

        // --- Spec equality semantics ---------------------------------------------------------------

        [Fact]
        public void LanesDifferingOnlyInWidthDoNotMatch() {
            var (source, _) = Make(Car(3f));
            var (dest, _) = Make(Car(3.5f));

            var mapping = LaneMapping.Derive(source, dest);

            //LaneSpec.Equals compares Width, so these are different lanes.
            Assert.Empty(mapping.Matched);
        }

        [Fact]
        public void LanesDifferingOnlyInDirectionDoNotMatch() {
            var (source, _) = Make(Car());
            var (dest, _) = Make(Car().Reverse());

            var mapping = LaneMapping.Derive(source, dest);

            //LaneSpec.Equals compares Flags, and Reverse flips IsMerge.
            Assert.Empty(mapping.Matched);
        }

        [Fact]
        public void CustomComparerCanIgnoreWidth() {
            var (source, _) = Make(Car(3f));
            var (dest, _) = Make(Car(3.5f));

            var mapping = LaneMapping.Derive(source, dest, static (a, b) => a.EqualsExceptWidth(b));

            Assert.Equal(1, mapping.Matched.Count);
        }

        [Fact]
        public void CustomComparerCanIgnoreDirection() {
            var (source, _) = Make(Car());
            var (dest, _) = Make(Car().Reverse());

            var mapping = LaneMapping.Derive(source, dest, static (a, b) => {
                var x = a; x.Flags = LaneFlags.None;
                var y = b; y.Flags = LaneFlags.None;
                return x.Equals(y);
            });

            Assert.Equal(1, mapping.Matched.Count);
        }

        // --- BuildDestinationOrder -----------------------------------------------------------------

        [Fact]
        public void DestinationOrderSplicesNewLanesInPlace() {
            var (source, _) = Make(Car(), Bus(), Tram(), Bike());
            var (dest, d) = Make(Car(), Tram(), Bike(), Foot());

            var mapping = LaneMapping.Derive(source, dest);
            var order = mapping.BuildDestinationOrder();

            //The footpath is spliced in after the bike lane, so it lands at the right end.
            Assert.Equal(new[] { d[0], d[1], d[2], d[3] }, order);
        }

        [Fact]
        public void DestinationOrderPlacesALeftInsertionAtTheFront() {
            var (source, _) = Make(Car(), Bus());
            var (dest, d) = Make(Foot(), Car(), Bus());

            var mapping = LaneMapping.Derive(source, dest);
            var order = mapping.BuildDestinationOrder();

            Assert.Equal(new[] { d[0], d[1], d[2] }, order);
        }

        [Fact]
        public void DestinationOrderOmitsSourceOnlyLanes() {
            var (source, s) = Make(Car(), Bus(), Tram());
            var (dest, d) = Make(Car(), Tram());

            var mapping = LaneMapping.Derive(source, dest);
            var order = mapping.BuildDestinationOrder();

            //The bus terminates at the node, so it has no destination counterpart and must not appear.
            //The order is checked by count and by membership of the destination lanes, because LaneId
            //values collide across drafts and so cannot be compared by value alone.
            Assert.Equal(new[] { d[0], d[1] }, order);
            Assert.Equal(dest.Count, order.Count);
            Assert.Equal(1, mapping.SourceOnly.Count);
            Assert.Equal(s[1], mapping.SourceOnly[0]);
        }

        [Fact]
        public void DestinationOrderContainsEveryDestinationLaneExactlyOnce() {
            var (source, _) = Make(Car(), Bus(), Tram(), Bike());
            var (dest, d) = Make(Car(), Tram(), Bike(), Foot());

            var mapping = LaneMapping.Derive(source, dest);
            var order = mapping.BuildDestinationOrder();

            Assert.Equal(dest.Count, order.Count);
            Assert.Equal(order.Count, order.Distinct().Count());
            foreach (var lane in d) Assert.Contains(lane, order);
        }

        // --- Editing -------------------------------------------------------------------------------

        [Fact]
        public void AddMatchPairsLanesAndRemovesThemFromTheOnlyLists() {
            var (source, s) = Make(Car(), Bus());
            var (dest, d) = Make(Tram(), Bike());

            var mapping = LaneMapping.Derive(source, dest);
            Assert.Equal(2, mapping.SourceOnly.Count);

            mapping.AddMatch(s[0], d[0]);

            Assert.Equal(1, mapping.Matched.Count);
            Assert.Equal(1, mapping.SourceOnly.Count);
            Assert.Equal(1, mapping.DestOnly.Count);
            Assert.Equal(d[0], mapping.DestFor(s[0]));
            Assert.Equal(s[0], mapping.SourceFor(d[0]));
        }

        [Fact]
        public void AddMatchRejectsADoublePairing() {
            var (source, s) = Make(Car(), Bus());
            var (dest, d) = Make(Car(), Bus());

            var mapping = LaneMapping.Derive(source, dest);

            Assert.Throws<InvalidOperationException>(() => mapping.AddMatch(s[0], d[1]));
        }

        [Fact]
        public void RemoveMatchReturnsBothLanesToTheOnlyLists() {
            var (source, s) = Make(Car(), Bus());
            var (dest, d) = Make(Car(), Bus());

            var mapping = LaneMapping.Derive(source, dest);
            Assert.True(mapping.RemoveMatch(s[0]));

            Assert.Equal(1, mapping.Matched.Count);
            Assert.Contains(s[0], mapping.SourceOnly);
            Assert.Contains(d[0], mapping.DestOnly);
            Assert.False(mapping.RemoveMatch(s[0]));
        }

        [Fact]
        public void AddInsertionRejectsALaneThatIsNotDestOnly() {
            var (source, s) = Make(Car());
            var (dest, d) = Make(Car());

            var mapping = LaneMapping.Derive(source, dest);

            Assert.Throws<InvalidOperationException>(
                () => mapping.AddInsertion(s[0], InsertionSide.Right, d[0]));
        }

        [Fact]
        public void AddInsertionReplacesAnEarlierInsertionForTheSameLane() {
            var (source, s) = Make(Car(), Bus());
            var (dest, d) = Make(Car(), Bus(), Tram());

            var mapping = LaneMapping.Derive(source, dest);
            Assert.Single(mapping.Insertions);

            mapping.AddInsertion(s[0], InsertionSide.Left, d[2]);

            Assert.Single(mapping.Insertions);
            Assert.Equal(s[0], mapping.InsertionFor(d[2])!.Value.Anchor);
            Assert.Equal(InsertionSide.Left, mapping.InsertionFor(d[2])!.Value.Side);
        }

        // --- Validation ----------------------------------------------------------------------------

        [Fact]
        public void ValidateAcceptsADerivedMapping() {
            var (source, _) = Make(Car(), Bus(), Tram(), Bike());
            var (dest, _) = Make(Car(), Tram(), Bike(), Foot());

            var mapping = LaneMapping.Derive(source, dest);

            mapping.Validate(source, dest);
        }

        [Fact]
        public void ValidateRejectsAnUnaccountedLane() {
            var (source, _) = Make(Car(), Bus());
            var (dest, _) = Make(Car(), Bus());

            var mapping = new LaneMapping();
            mapping.AddMatch(source[0].Id, dest[0].Id);
            //source[1] and dest[1] are deliberately left unaccounted for.

            Assert.Throws<InvalidOperationException>(() => mapping.Validate(source, dest));
        }

        [Fact]
        public void ValidateRejectsADestOnlyLaneWithNoInsertionPoint() {
            var (source, s) = Make(Car());
            var (dest, d) = Make(Car(), Tram());

            var mapping = new LaneMapping();
            mapping.AddMatch(s[0], d[0]);
            mapping.AddDestOnly(d[1]);
            //No insertion recorded for d[1].

            Assert.Throws<InvalidOperationException>(() => mapping.Validate(source, dest));
        }

        [Fact]
        public void ValidateRejectsALaneThatIsNotInTheSourceDraft() {
            var (source, _) = Make(Car());
            var (dest, _) = Make(Car());
            //A second draft issues its own LaneIds, so its second lane is LaneId(1) - a value that does
            //not exist in the one-lane source draft.
            var (other, otherIds) = Make(Car(), Bus());

            var mapping = new LaneMapping();
            mapping.AddMatch(source[0].Id, dest[0].Id);
            mapping.AddSourceOnly(otherIds[1]);

            Assert.Throws<InvalidOperationException>(() => mapping.Validate(source, dest));
        }

        [Fact]
        public void LaneIdsAreOnlyUniqueWithinADraft() {
            //Documents the boundary of what a mapping can detect: LaneId is a per-draft identity, so two
            //drafts both issue LaneId(0) and a mapping cannot tell them apart by value alone.
            var (first, firstIds) = Make(Car());
            var (second, secondIds) = Make(Bus());

            Assert.Equal(firstIds[0], secondIds[0]);
            Assert.NotEqual(first[0].Spec, second[0].Spec);
        }

        // --- Integration with the draft ------------------------------------------------------------

        [Fact]
        public void MappingSurvivesAnInsertionInTheSourceDraft() {
            var (source, s) = Make(Car(), Tram());
            var (dest, d) = Make(Car(), Tram());

            var mapping = LaneMapping.Derive(source, dest);

            //Insert a lane in the middle of the source. Because lanes are referred to by identity, the
            //mapping is not invalidated by its own application - the property the positional
            //LaneMappingInputs could not provide.
            var inserted = source.Insert(1, Bus());

            Assert.Equal(d[0], mapping.DestFor(s[0]));
            Assert.Equal(d[1], mapping.DestFor(s[1]));
            Assert.False(mapping.DestFor(inserted).HasValue);
        }

        [Fact]
        public void MappingSurvivesARemovalInTheSourceDraft() {
            var (source, s) = Make(Car(), Bus(), Tram());
            var (dest, d) = Make(Car(), Bus(), Tram());

            var mapping = LaneMapping.Derive(source, dest);
            source.Remove(s[1]);

            Assert.Equal(d[0], mapping.DestFor(s[0]));
            Assert.Equal(d[2], mapping.DestFor(s[2]));
        }

        [Fact]
        public void DeriveRejectsNullArguments() {
            var draft = new NodeSpecDraft();
            Assert.Throws<ArgumentNullException>(() => LaneMapping.Derive(null!, draft));
            Assert.Throws<ArgumentNullException>(() => LaneMapping.Derive(draft, null!));
        }

        [Fact]
        public void DeriveRejectsANullComparer() {
            var draft = new NodeSpecDraft();
            Assert.Throws<ArgumentNullException>(() => LaneMapping.Derive(draft, draft, null!));
        }

        [Fact]
        public void DeriveIsStableAcrossRepeatedCalls() {
            var (source, _) = Make(Car(), Bus(), Tram(), Bike());
            var (dest, _) = Make(Car(), Tram(), Bike(), Foot());

            var first = LaneMapping.Derive(source, dest);
            var second = LaneMapping.Derive(source, dest);

            Assert.Equal(first.Matched, second.Matched);
            Assert.Equal(first.SourceOnly, second.SourceOnly);
            Assert.Equal(first.DestOnly, second.DestOnly);
            Assert.Equal(first.Insertions, second.Insertions);
        }

        [Fact]
        public void DeriveHandlesAWideCrossSection() {
            //A stress case at the upper end of realistic road widths.
            var specs = new LaneSpec[24];
            for (int i = 0; i < specs.Length; i++) specs[i] = Car(2f + i * 0.1f);
            var (source, _) = Make(specs);

            var destSpecs = new LaneSpec[24];
            for (int i = 0; i < destSpecs.Length; i++) destSpecs[i] = Car(2f + i * 0.1f);
            var (dest, _) = Make(destSpecs);

            var mapping = LaneMapping.Derive(source, dest);

            Assert.True(mapping.IsComplete);
            Assert.Equal(24, mapping.Matched.Count);
        }
    }
}
