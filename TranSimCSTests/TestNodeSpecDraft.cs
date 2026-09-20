using TranSimCS;
using TranSimCS.Geometry;
using TranSimCS.Mode.RoadBuilder;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;

namespace TranSimCSTests {
    public class TestNodeSpecDraft {
        private static LaneSpec Spec(float width = 3f, VehicleTypes types = VehicleTypes.Car, float speed = 50)
            => new(Colors.Gray, types, width, speed);

        // --- Construction and identity -------------------------------------------------------------

        [Fact]
        public void InsertReturnsDistinctIdentities() {
            var draft = new NodeSpecDraft();
            var a = draft.Insert(0, Spec());
            var b = draft.Insert(1, Spec());
            var c = draft.Insert(1, Spec());

            Assert.NotEqual(a, b);
            Assert.NotEqual(a, c);
            Assert.NotEqual(b, c);
            Assert.Equal(3, draft.Count);
        }

        [Fact]
        public void InsertAtEveryIndexProducesThatOrder() {
            for (int target = 0; target <= 4; target++) {
                var draft = new NodeSpecDraft();
                var ids = new List<LaneId>();
                for (int i = 0; i < 4; i++) ids.Add(draft.Add(Spec()));

                var inserted = draft.Insert(target, Spec());
                Assert.Equal(target, draft.IndexOf(inserted));

                //The pre-existing lanes keep their relative order.
                var remaining = ids.Where(id => draft.Contains(id)).ToList();
                var expected = new List<LaneId>(ids);
                expected.Insert(target, inserted);
                Assert.Equal(expected, draft.Select(x => x.Id).ToList());
            }
        }

        [Fact]
        public void InsertOutOfRangeThrows() {
            var draft = new NodeSpecDraft();
            draft.Add(Spec());
            Assert.Throws<ArgumentOutOfRangeException>(() => draft.Insert(-1, Spec()));
            Assert.Throws<ArgumentOutOfRangeException>(() => draft.Insert(2, Spec()));
        }

        [Fact]
        public void InsertionDoesNotInvalidateOtherIdentities() {
            var draft = new NodeSpecDraft();
            var left = draft.Add(Spec());
            var right = draft.Add(Spec());

            //Insert in the middle: the identities of the existing lanes must still resolve.
            var middle = draft.Insert(1, Spec());

            Assert.Equal(0, draft.IndexOf(left));
            Assert.Equal(1, draft.IndexOf(middle));
            Assert.Equal(2, draft.IndexOf(right));
        }

        [Fact]
        public void RemoveKeepsOtherIdentitiesValid() {
            var draft = new NodeSpecDraft();
            var a = draft.Add(Spec());
            var b = draft.Add(Spec());
            var c = draft.Add(Spec());

            Assert.True(draft.Remove(b));
            Assert.False(draft.Remove(b));

            Assert.Equal(0, draft.IndexOf(a));
            Assert.Equal(1, draft.IndexOf(c));
            Assert.Equal(2, draft.Count);
        }

        [Fact]
        public void MoveReordersWithoutChangingIdentities() {
            var draft = new NodeSpecDraft();
            var a = draft.Add(Spec());
            var b = draft.Add(Spec());
            var c = draft.Add(Spec());

            draft.Move(a, 2);

            Assert.Equal(new[] { b, c, a }, draft.Select(x => x.Id).ToArray());
        }

        [Fact]
        public void SetSpecKeepsIdentityAndPosition() {
            var draft = new NodeSpecDraft();
            var a = draft.Add(Spec());
            var b = draft.Add(Spec());

            var replacement = Spec(5f, VehicleTypes.Bus);
            draft.SetSpec(a, replacement);

            Assert.Equal(0, draft.IndexOf(a));
            Assert.Equal(replacement, draft.Get(a).Spec);
            Assert.Equal(1, draft.IndexOf(b));
        }

        // --- Offset computation --------------------------------------------------------------------

        [Fact]
        public void OffsetsAreContiguousAndCentred() {
            var draft = new NodeSpecDraft();
            draft.Add(Spec(3f));
            draft.Add(Spec(3f));
            draft.Add(Spec(3f));

            var offsets = draft.ComputeOffsets();

            Assert.Equal(new[] { -3f, 0f, 3f }, offsets);
        }

        [Fact]
        public void OffsetsRespectUnequalWidths() {
            var draft = new NodeSpecDraft();
            draft.Add(Spec(2f));
            draft.Add(Spec(4f));

            var offsets = draft.ComputeOffsets();

            //Total width 6, so the cross-section runs from -3 to 3.
            Assert.Equal(-2f, offsets[0]);
            Assert.Equal(1f, offsets[1]);
        }

        [Fact]
        public void OffsetsAreCentredOnTheCenterline() {
            var draft = new NodeSpecDraft(10f);
            draft.Add(Spec(3f));
            draft.Add(Spec(3f));

            var offsets = draft.ComputeOffsets();

            Assert.Equal(new[] { 8.5f, 11.5f }, offsets);
            Assert.Equal(10f, draft.Range.Middle());
        }

        [Fact]
        public void AsymmetricSpecIsExpressible() {
            //Three lanes one way, one the other: the centreline sits between lane 2 and lane 3.
            var draft = new NodeSpecDraft();
            draft.Add(Spec(3f));
            draft.Add(Spec(3f));
            draft.Add(Spec(3f));
            draft.Add(Spec(3f));

            var offsets = draft.ComputeOffsets();
            Assert.Equal(new[] { -4.5f, -1.5f, 1.5f, 4.5f }, offsets);

            //Shifting the centreline moves the whole cross-section without changing the lane order.
            draft.Centerline = 1.5f;
            Assert.Equal(new[] { -3f, 0f, 3f, 6f }, draft.ComputeOffsets());
        }

        [Fact]
        public void WideningALanePushesNeighboursOutward() {
            var draft = new NodeSpecDraft();
            var left = draft.Add(Spec(3f));
            var middle = draft.Add(Spec(3f));
            var right = draft.Add(Spec(3f));

            var before = draft.ComputeOffsets();
            Assert.Equal(new[] { -3f, 0f, 3f }, before);

            draft.SetSpec(middle, Spec(5f));
            var after = draft.ComputeOffsets();

            //The widened lane stays centred; the neighbours move outward, they do not overlap it.
            Assert.Equal(0f, after[1]);
            Assert.True(after[0] < before[0]);
            Assert.True(after[2] > before[2]);

            var bounds = draft.ComputeBounds();
            Assert.Equal(bounds[0].Max, bounds[1].Min);
            Assert.Equal(bounds[1].Max, bounds[2].Min);
        }

        [Fact]
        public void BoundsAreContiguous() {
            var draft = new NodeSpecDraft();
            draft.Add(Spec(2f));
            draft.Add(Spec(3.5f));
            draft.Add(Spec(1.5f));

            var bounds = draft.ComputeBounds();
            for (int i = 0; i + 1 < bounds.Length; i++)
                Assert.Equal(bounds[i].Max, bounds[i + 1].Min);

            Assert.Equal(draft.Range.Min, bounds[0].Min);
            Assert.Equal(draft.Range.Max, bounds[^1].Max);
        }

        [Fact]
        public void ForcedWidthScalesLanesToFit() {
            var draft = new NodeSpecDraft { ForcedWidth = 12f };
            draft.Add(Spec(3f));
            draft.Add(Spec(3f));

            var bounds = draft.ComputeBounds();
            Assert.Equal(-6f, bounds[0].Min);
            Assert.Equal(6f, bounds[^1].Max);
            Assert.Equal(12f, draft.TotalWidth);
        }

        [Fact]
        public void EmptyDraftHasDegenerateRange() {
            var draft = new NodeSpecDraft(2f);
            Assert.Equal(new Interval<float>(2f, 2f), draft.Range);
            Assert.Empty(draft.ComputeOffsets());
        }

        // --- Exit ----------------------------------------------------------------------------------

        [Fact]
        public void ExitRightInsertsBesideWithoutMovingTheOriginal() {
            var draft = new NodeSpecDraft();
            var left = draft.Add(Spec(3f));
            var original = draft.Add(Spec(3f));
            var right = draft.Add(Spec(3f));

            var before = draft.ComputeOffsets();
            var originalOffsetBefore = before[draft.IndexOf(original)];

            var exit = draft.Exit(original, side: 1);

            Assert.Equal(1, draft.IndexOf(original));
            Assert.Equal(2, draft.IndexOf(exit));
            Assert.Equal(4, draft.Count);

            //The original lane's own offset is unchanged.
            var after = draft.ComputeOffsets();
            Assert.Equal(originalOffsetBefore, after[draft.IndexOf(original)]);

            //The copy inherits the original's spec.
            Assert.Equal(draft.Get(original).Spec, draft.Get(exit).Spec);
        }

        [Fact]
        public void ExitLeftInsertsBesideWithoutMovingTheOriginal() {
            var draft = new NodeSpecDraft();
            var original = draft.Add(Spec(3f));
            var right = draft.Add(Spec(3f));

            var originalOffsetBefore = draft.ComputeOffsets()[0];
            var exit = draft.Exit(original, side: -1);

            Assert.Equal(0, draft.IndexOf(exit));
            Assert.Equal(1, draft.IndexOf(original));
            Assert.Equal(originalOffsetBefore, draft.ComputeOffsets()[1]);
        }

        [Fact]
        public void ExitOnlyShiftsLanesBeyondTheInsertionPoint() {
            var draft = new NodeSpecDraft();
            var a = draft.Add(Spec(3f));
            var b = draft.Add(Spec(3f));
            var c = draft.Add(Spec(3f));

            var before = draft.ComputeOffsets();
            draft.Exit(b, side: 1);
            var after = draft.ComputeOffsets();

            //Lanes before the insertion point keep their offsets exactly.
            Assert.Equal(before[0], after[0]);
            Assert.Equal(before[1], after[1]);
            //The lane beyond it is pushed outward.
            Assert.True(after[3] > before[2]);
        }

        [Fact]
        public void ExitAcceptsAnOverrideSpec() {
            var draft = new NodeSpecDraft();
            var original = draft.Add(Spec(3f, VehicleTypes.Car));

            var exit = draft.Exit(original, side: 1, overrideSpec: Spec(2f, VehicleTypes.Bicycle));

            Assert.Equal(VehicleTypes.Bicycle, draft.Get(exit).Spec.VehicleTypes);
            Assert.Equal(2f, draft.Get(exit).Spec.Width);
            Assert.Equal(VehicleTypes.Car, draft.Get(original).Spec.VehicleTypes);
        }

        [Fact]
        public void ExitWithZeroSideThrows() {
            var draft = new NodeSpecDraft();
            var lane = draft.Add(Spec());
            Assert.Throws<ArgumentException>(() => draft.Exit(lane, side: 0));
        }

        // --- Merge and split -----------------------------------------------------------------------

        [Fact]
        public void MergeUnionsVehicleTypes() {
            var draft = new NodeSpecDraft();
            var car = draft.Add(Spec(3f, VehicleTypes.Car, 50));
            var tram = draft.Add(Spec(3.5f, VehicleTypes.LRT, 80));

            var merged = draft.Merge(0);

            Assert.Equal(1, draft.Count);
            Assert.Equal(merged, draft[0].Id);
            Assert.Equal(VehicleTypes.Car | VehicleTypes.LRT, draft[0].Spec.VehicleTypes);
            Assert.Equal(3.5f, draft[0].Spec.Width);
            Assert.Equal(50f, draft[0].Spec.SpeedLimit);
        }

        [Fact]
        public void MergeAtLastIndexThrows() {
            var draft = new NodeSpecDraft();
            draft.Add(Spec());
            Assert.Throws<ArgumentOutOfRangeException>(() => draft.Merge(0));
        }

        [Fact]
        public void SplitDividesTheWidth() {
            var draft = new NodeSpecDraft();
            draft.Add(Spec(4f));

            var (left, right) = draft.Split(0, 0.25f);

            Assert.Equal(2, draft.Count);
            Assert.Equal(1f, draft.Get(left).Spec.Width);
            Assert.Equal(3f, draft.Get(right).Spec.Width);
            Assert.Equal(4f, draft.TotalWidth);
        }

        [Fact]
        public void SplitRejectsOutOfRangeFraction() {
            var draft = new NodeSpecDraft();
            draft.Add(Spec());
            Assert.Throws<ArgumentOutOfRangeException>(() => draft.Split(0, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => draft.Split(0, 1f));
        }

        // --- Direction -----------------------------------------------------------------------------

        [Fact]
        public void ToggleDirectionFlipsTheMergeFlag() {
            var draft = new NodeSpecDraft();
            var lane = draft.Add(Spec());

            var before = draft.Get(lane).Spec.Flags;
            draft.ToggleDirection(lane);
            var after = draft.Get(lane).Spec.Flags;

            Assert.NotEqual(before, after);
            Assert.Equal(before, after.Reverse());

            //Toggling twice returns to the original.
            draft.ToggleDirection(lane);
            Assert.Equal(before, draft.Get(lane).Spec.Flags);
        }

        [Fact]
        public void ToggleDirectionSwapsLeftAndRightFlags() {
            var draft = new NodeSpecDraft();
            var spec = Spec();
            spec.Flags = LaneFlags.NoLeft;
            var lane = draft.Add(spec);

            draft.ToggleDirection(lane);

            Assert.True(draft.Get(lane).Spec.Flags.HasFlag(LaneFlags.NoRight));
            Assert.False(draft.Get(lane).Spec.Flags.HasFlag(LaneFlags.NoLeft));
        }

        // --- Conversion ----------------------------------------------------------------------------

        [Fact]
        public void ToNodeSpecProducesTheDerivedOffsets() {
            var draft = new NodeSpecDraft();
            draft.Add(Spec(3f));
            draft.Add(Spec(3f));
            draft.Add(Spec(3f));

            var spec = draft.ToNodeSpec();

            Assert.Equal(3, spec.Lanes.Length);
            Assert.Equal(new[] { -3f, 0f, 3f }, spec.Lanes.Select(x => x.CenterPos).ToArray());
            Assert.Equal(new Interval<float>(-4.5f, 4.5f), spec.Range);
        }

        [Fact]
        public void ToNodeSpecAssignsFreshGuids() {
            var draft = new NodeSpecDraft();
            draft.Add(Spec());
            draft.Add(Spec());

            var first = draft.ToNodeSpec();
            var second = draft.ToNodeSpec();

            //The draft's LaneIds are tool-side identities and must not leak into the world model.
            Assert.Empty(first.LaneXRef.Keys.Intersect(second.LaneXRef.Keys));
        }

        [Fact]
        public void RoundTripPreservesLaneOrderAndSpecs() {
            var draft = new NodeSpecDraft();
            draft.Add(Spec(2f, VehicleTypes.Bicycle, 30));
            draft.Add(Spec(3.5f, VehicleTypes.MotorVehicles, 150));
            draft.Add(Spec(1.5f, VehicleTypes.Pedestrian, 16));

            var spec = draft.ToNodeSpec();
            var roundTripped = NodeSpecDraft.FromNodeSpec(spec);

            Assert.Equal(draft.Count, roundTripped.Count);
            for (int i = 0; i < draft.Count; i++) {
                Assert.Equal(draft[i].Spec.VehicleTypes, roundTripped[i].Spec.VehicleTypes);
                Assert.Equal(draft[i].Spec.Width, roundTripped[i].Spec.Width);
                Assert.Equal(draft[i].Spec.SpeedLimit, roundTripped[i].Spec.SpeedLimit);
                Assert.Equal(draft[i].Spec.Flags, roundTripped[i].Spec.Flags);
                Assert.Equal(draft[i].Spec.Surface, roundTripped[i].Spec.Surface);
                Assert.Equal(draft[i].Spec.Color, roundTripped[i].Spec.Color);
                Assert.Equal(draft[i].Spec.LineWidth, roundTripped[i].Spec.LineWidth);
            }
        }

        [Fact]
        public void RoundTripPreservesEveryLaneSpecField() {
            var spec = new LaneSpec(
                color: new Color(12, 34, 56, 78),
                vehicleTypes: VehicleTypes.Car | VehicleTypes.LRT,
                width: 4.25f,
                speedLimit: 73.5f,
                flags: LaneFlags.Parking | LaneFlags.NoLeft,
                lineWidth: 0.35f,
                surface: Surface.Cobble
            );

            var draft = new NodeSpecDraft();
            draft.Add(spec);

            var result = NodeSpecDraft.FromNodeSpec(draft.ToNodeSpec())[0].Spec;

            Assert.Equal(spec.Color, result.Color);
            Assert.Equal(spec.VehicleTypes, result.VehicleTypes);
            Assert.Equal(spec.Width, result.Width);
            Assert.Equal(spec.SpeedLimit, result.SpeedLimit);
            Assert.Equal(spec.Flags, result.Flags);
            Assert.Equal(spec.LineWidth, result.LineWidth);
            Assert.Equal(spec.Surface, result.Surface);
        }

        [Fact]
        public void RoundTripOfEmptyDraftIsEmpty() {
            var draft = new NodeSpecDraft();
            var roundTripped = NodeSpecDraft.FromNodeSpec(draft.ToNodeSpec());
            Assert.Equal(0, roundTripped.Count);
        }

        [Fact]
        public void FromNodeSpecPreservesOrderNotJustMembership() {
            var draft = new NodeSpecDraft();
            var first = draft.Add(Spec(3f, VehicleTypes.Car));
            var second = draft.Add(Spec(3f, VehicleTypes.Bus));
            var third = draft.Add(Spec(3f, VehicleTypes.LRT));

            var roundTripped = NodeSpecDraft.FromNodeSpec(draft.ToNodeSpec());

            Assert.Equal(VehicleTypes.Car, roundTripped[0].Spec.VehicleTypes);
            Assert.Equal(VehicleTypes.Bus, roundTripped[1].Spec.VehicleTypes);
            Assert.Equal(VehicleTypes.LRT, roundTripped[2].Spec.VehicleTypes);
        }

        // --- Clone and mirror ----------------------------------------------------------------------

        [Fact]
        public void CloneIsIndependent() {
            var draft = new NodeSpecDraft(1f);
            var lane = draft.Add(Spec(3f));

            var clone = draft.Clone();
            clone.SetSpec(lane, Spec(9f));
            clone.Add(Spec());

            Assert.Equal(3f, draft.Get(lane).Spec.Width);
            Assert.Equal(1, draft.Count);
            Assert.Equal(2, clone.Count);
            Assert.Equal(1f, clone.Centerline);
        }

        [Fact]
        public void CloneKeepsIdentities() {
            var draft = new NodeSpecDraft();
            var lane = draft.Add(Spec());

            var clone = draft.Clone();

            Assert.True(clone.Contains(lane));
            Assert.Equal(0, clone.IndexOf(lane));
        }

        [Fact]
        public void MirrorReversesOrder() {
            var draft = new NodeSpecDraft();
            var a = draft.Add(Spec(3f, VehicleTypes.Car));
            var b = draft.Add(Spec(3f, VehicleTypes.Bus));

            draft.Mirror();

            Assert.Equal(new[] { b, a }, draft.Select(x => x.Id).ToArray());
        }

        // --- Half-node integration -----------------------------------------------------------------

        [Fact]
        public void FromHalfNodeReadsLeftToRight() {
            var node = new RoadNode("node", PositionEulerAngles.Zero);
            node.AddLane(new LaneNode(Spec(3f, VehicleTypes.Car), -1.5f));
            node.AddLane(new LaneNode(Spec(3f, VehicleTypes.Bus), 1.5f));

            var front = NodeSpecDraft.FromHalfNode(node.FrontHalf);
            Assert.Equal(VehicleTypes.Car, front[0].Spec.VehicleTypes);
            Assert.Equal(VehicleTypes.Bus, front[1].Spec.VehicleTypes);

            //The rear half is mirrored, so its own left-to-right order is the reverse.
            var rear = NodeSpecDraft.FromHalfNode(node.RearHalf);
            Assert.Equal(VehicleTypes.Bus, rear[0].Spec.VehicleTypes);
            Assert.Equal(VehicleTypes.Car, rear[1].Spec.VehicleTypes);
        }

        [Fact]
        public void DraftBuiltFromHalfNodeRoundTripsThroughTheNode() {
            var node = new RoadNode("node", PositionEulerAngles.Zero);
            node.AddLane(new LaneNode(Spec(3f, VehicleTypes.Car), -1.5f));
            node.AddLane(new LaneNode(Spec(3f, VehicleTypes.Bus), 1.5f));

            var draft = NodeSpecDraft.FromHalfNode(node.FrontHalf);
            var spec = draft.ToNodeSpec();

            Assert.Equal(2, spec.Lanes.Length);
            Assert.Equal(new[] { -1.5f, 1.5f }, spec.Lanes.Select(x => x.CenterPos).ToArray());
        }
    }
}
