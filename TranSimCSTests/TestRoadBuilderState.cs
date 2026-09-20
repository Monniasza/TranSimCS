using TranSimCS;
using TranSimCS.Mode.RoadBuilder;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;

namespace TranSimCSTests {
    /// <summary>
    /// Covers the Road Builder's state machine and the <see cref="LaneHit"/> pick result. The renderer and
    /// the ImGui panels need a GPU context and are not covered here.
    /// </summary>
    public class TestRoadBuilderState {
        private static LaneSpec Spec(float width = 3f, VehicleTypes types = VehicleTypes.Car)
            => new(Colors.Gray, types, width, 50);

        private static RoadNode MakeNode(params float[] centers) {
            var node = new RoadNode("node", PositionEulerAngles.Zero);
            foreach (var center in centers) node.AddLane(new LaneNode(Spec(), center));
            return node;
        }

        // --- Phase transitions ---------------------------------------------------------------------

        [Fact]
        public void ANewStateIsIdle() {
            var state = new RoadBuilderState();

            Assert.Equal(RoadBuilderPhase.Idle, state.Phase);
            Assert.Null(state.SourceEnd);
            Assert.Null(state.Draft);
            Assert.False(state.HasDraft);
        }

        [Fact]
        public void BeginFromMovesToSourcePickedAndCopiesTheLanes() {
            var node = MakeNode(-1.5f, 1.5f);
            var state = new RoadBuilderState();

            state.BeginFrom(node.FrontEnd);

            Assert.Equal(RoadBuilderPhase.SourcePicked, state.Phase);
            Assert.Equal(node.FrontEnd, state.SourceEnd);
            Assert.NotNull(state.Draft);
            Assert.Equal(2, state.Draft!.Count);
            Assert.True(state.HasDraft);
        }

        [Fact]
        public void BeginFromSnapshotsTheSourceSeparatelyFromTheDraft() {
            var node = MakeNode(-1.5f, 1.5f);
            var state = new RoadBuilderState();

            state.BeginFrom(node.FrontEnd);

            //The draft is a copy, so editing it must not disturb the source snapshot.
            Assert.NotNull(state.SourceDraft);
            Assert.NotSame(state.SourceDraft, state.Draft);
            state.Draft!.Add(Spec(4f));
            Assert.Equal(2, state.SourceDraft!.Count);
            Assert.Equal(3, state.Draft.Count);
        }

        [Fact]
        public void BeginFromRejectsNull() {
            var state = new RoadBuilderState();
            Assert.Throws<ArgumentNullException>(() => state.BeginFrom(null!));
        }

        [Fact]
        public void StepBackFromSourcePickedResetsToIdle() {
            var node = MakeNode(-1.5f, 1.5f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);

            state.StepBack();

            Assert.Equal(RoadBuilderPhase.Idle, state.Phase);
            Assert.Null(state.Draft);
            Assert.Null(state.SourceEnd);
        }

        [Fact]
        public void StepBackWalksTheChainOneStepAtATime() {
            var node = MakeNode(-1.5f, 1.5f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);

            state.Phase = RoadBuilderPhase.Dragging;
            state.StepBack();
            Assert.Equal(RoadBuilderPhase.SourcePicked, state.Phase);

            state.Phase = RoadBuilderPhase.Placing;
            state.StepBack();
            Assert.Equal(RoadBuilderPhase.Dragging, state.Phase);

            state.Phase = RoadBuilderPhase.Connecting;
            state.StepBack();
            Assert.Equal(RoadBuilderPhase.Dragging, state.Phase);
        }

        [Fact]
        public void StepBackFromIdleIsHarmless() {
            var state = new RoadBuilderState();
            state.StepBack();
            Assert.Equal(RoadBuilderPhase.Idle, state.Phase);
        }

        [Fact]
        public void ResetClearsEverything() {
            var node = MakeNode(-1.5f, 1.5f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            state.Phase = RoadBuilderPhase.Connecting;
            state.HoveredLane = state.Draft![0].Id;
            state.Mirror = true;

            state.Reset();

            Assert.Equal(RoadBuilderPhase.Idle, state.Phase);
            Assert.Null(state.SourceEnd);
            Assert.Null(state.Draft);
            Assert.Null(state.SourceDraft);
            Assert.Null(state.Mapping);
            Assert.Null(state.HoveredLane);
            Assert.False(state.Mirror);
        }

        // --- Mapping -------------------------------------------------------------------------------

        [Fact]
        public void RefreshMappingDerivesAgainstTheSourceSnapshot() {
            var node = MakeNode(-1.5f, 1.5f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);

            state.RefreshMapping();

            Assert.NotNull(state.Mapping);
            Assert.True(state.Mapping!.IsComplete);
            Assert.Equal(2, state.Mapping.Matched.Count);
        }

        [Fact]
        public void RefreshMappingWithoutADraftClearsTheMapping() {
            var state = new RoadBuilderState();
            state.RefreshMapping();
            Assert.Null(state.Mapping);
        }

        [Fact]
        public void RefreshMappingReflectsEditsToTheDraft() {
            var node = MakeNode(-1.5f, 1.5f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            state.RefreshMapping();
            Assert.Equal(2, state.Mapping!.Matched.Count);

            //Add a lane to the draft: it becomes destination-only.
            state.Draft!.Add(Spec(2f, VehicleTypes.Bicycle));
            state.RefreshMapping();

            Assert.Equal(2, state.Mapping!.Matched.Count);
            Assert.Single(state.Mapping.DestOnly);
        }

        [Fact]
        public void RefreshMappingUsesTheSnapshotNotTheEditedDraft() {
            var node = MakeNode(-1.5f, 1.5f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);

            //Editing the draft must not change what the mapping is derived against.
            state.Draft!.Add(Spec(2f, VehicleTypes.Bicycle));
            state.RefreshMapping();

            Assert.Equal(2, state.SourceDraft!.Count);
            Assert.Equal(3, state.Draft.Count);
            Assert.Equal(2, state.Mapping!.Matched.Count);
        }

        // --- LaneHit -------------------------------------------------------------------------------

        [Fact]
        public void LaneHitCarriesTheLaneIdentityAndIndex() {
            var node = MakeNode(-1.5f, 1.5f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var id = state.Draft![1].Id;

            var hit = new LaneHit(id, 1, 1.5f, node.FrontEnd);

            Assert.Equal(id, hit.Id);
            Assert.Equal(1, hit.Index);
            Assert.Equal(1.5f, hit.Offset);
            Assert.False(hit.IsInsertion);
            Assert.Equal(node.FrontEnd, hit.NodeEnd);
        }

        [Fact]
        public void InsertionHitHasNoLaneIdentity() {
            var node = MakeNode(-1.5f, 1.5f);

            var hit = LaneHit.Insertion(1, 0f, node.FrontEnd);

            Assert.True(hit.IsInsertion);
            Assert.Equal(1, hit.Index);
            Assert.Equal(0f, hit.Offset);
        }

        [Fact]
        public void LaneHitComputesTheOffsetsANewLaneWouldOccupy() {
            var node = MakeNode(-1.5f, 1.5f);
            var hit = LaneHit.Insertion(1, 0f, node.FrontEnd);

            var offsets = hit.CalculateOffsets(3f);

            //A 3 m lane centred on the hit spans -1.5 to 1.5.
            Assert.Equal(-1.5f, offsets.Min);
            Assert.Equal(1.5f, offsets.Max);
        }

        [Fact]
        public void LaneHitReportsTheNodeEndItBelongsTo() {
            var node = MakeNode(-1.5f, 1.5f);
            var hit = LaneHit.Insertion(0, 0f, node.RearEnd);

            Assert.Equal(node, hit.GetRoadNode());
            Assert.Equal(node.RearEnd, hit.GetNodeEnd());
            Assert.Equal(0, hit.GetIndexInHalfNode());
        }

        [Fact]
        public void LaneHitHasNoGuidBecauseItIsNotAWorldObject() {
            var node = MakeNode(-1.5f, 1.5f);
            var hit = LaneHit.Insertion(0, 0f, node.FrontEnd);

            //A LaneHit is a transient pick result; asking for a Guid is a programming error.
            Assert.Throws<NotSupportedException>(() => hit.Guid);
        }

        [Fact]
        public void LaneHitIsNotALaneOrStrip() {
            var node = MakeNode(-1.5f, 1.5f);
            var hit = LaneHit.Insertion(0, 0f, node.FrontEnd);

            Assert.Null(hit.GetLane());
            Assert.Null(hit.GetLaneEnd());
            Assert.Null(hit.GetLaneStrip());
            Assert.Null(hit.GetRoadStrip());
        }

        // --- Integration with the draft ------------------------------------------------------------

        [Fact]
        public void DraftFromTheSourceEndMatchesTheNodesLanes() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();

            state.BeginFrom(node.FrontEnd);

            Assert.Equal(3, state.Draft!.Count);
            var offsets = state.Draft.ComputeOffsets();
            Assert.Equal(new[] { -3f, 0f, 3f }, offsets);
        }

        [Fact]
        public void DraftFromTheRearEndIsMirrored() {
            var node = new RoadNode("node", PositionEulerAngles.Zero);
            node.AddLane(new LaneNode(Spec(3f, VehicleTypes.Car), -1.5f));
            node.AddLane(new LaneNode(Spec(3f, VehicleTypes.Bus), 1.5f));

            var state = new RoadBuilderState();
            state.BeginFrom(node.RearEnd);

            //The rear half's own left-to-right order runs opposite to the node's.
            Assert.Equal(VehicleTypes.Bus, state.Draft![0].Spec.VehicleTypes);
            Assert.Equal(VehicleTypes.Car, state.Draft[1].Spec.VehicleTypes);
        }

        [Fact]
        public void MirrorReversesTheDraftOrder() {
            var node = new RoadNode("node", PositionEulerAngles.Zero);
            node.AddLane(new LaneNode(Spec(3f, VehicleTypes.Car), -1.5f));
            node.AddLane(new LaneNode(Spec(3f, VehicleTypes.Bus), 1.5f));

            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var first = state.Draft![0].Id;

            state.Draft.Mirror();

            Assert.Equal(1, state.Draft.IndexOf(first));
        }
    }
}
