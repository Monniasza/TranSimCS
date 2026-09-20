using System;
using System.Numerics;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCS.Mode.RoadBuilder {
    /// <summary>
    /// The steps of the Road Builder's interaction, following the state machine in the design document,
    /// section 3.2:
    /// <code>
    /// Idle
    ///  └─ pick source node end ──────────────► SourcePicked
    ///       └─ drag ─────────────────────────► Dragging      (live preview, spec editable)
    ///            └─ release on empty ────────► Placing     (commit on click)
    ///            └─ release on node ─────────► Connecting  (mapping preview, commit on click)
    ///                 └─ Esc / right-click ──► back one step
    /// </code>
    /// </summary>
    public enum RoadBuilderPhase {
        /// <summary>Nothing picked yet; the tool is waiting for a source node end.</summary>
        Idle,
        /// <summary>A source node end has been picked; the draft exists and can be edited.</summary>
        SourcePicked,
        /// <summary>The user is dragging from the source; the preview follows the cursor.</summary>
        Dragging,
        /// <summary>Released over empty space; the next click commits a new node.</summary>
        Placing,
        /// <summary>Released over an existing node; the next click commits a connection.</summary>
        Connecting
    }

    /// <summary>
    /// The Road Builder's tool state: which step the interaction is at, the draft being edited, and the
    /// transient selection and drag state.
    /// <para>
    /// The draft is the document (design document, section 2). Everything the tool does is a mutation of
    /// <see cref="Draft"/>; the world is only touched when the draft is committed.
    /// </para>
    /// </summary>
    public sealed class RoadBuilderState {
        /// <summary>The step of the interaction.</summary>
        public RoadBuilderPhase Phase { get; set; } = RoadBuilderPhase.Idle;

        /// <summary>
        /// The order-corrected half-node the draft is being built from. Null while
        /// <see cref="RoadBuilderPhase.Idle"/>.
        /// <para>
        /// This is a <see cref="HalfNode"/> rather than a <see cref="RoadNodeEnd"/> on purpose:
        /// <see cref="RoadNodeEnd"/> is not order-corrected, so indexing lanes through it silently
        /// mirrors for the <c>Backward</c> end. <see cref="HalfNode"/> is the order-corrected view.
        /// </para>
        /// </summary>
        public HalfNode? SourceHalfNode { get; set; }

        /// <summary>The spec being built. Null while <see cref="RoadBuilderPhase.Idle"/>.</summary>
        public NodeSpecDraft? Draft { get; set; }

        /// <summary>
        /// A snapshot of the source node's spec, taken when the source was picked. Kept so the mapping can
        /// be re-derived against the original even after the draft has been edited.
        /// </summary>
        public NodeSpecDraft? SourceDraft { get; set; }

        /// <summary>
        /// The mapping between the source and the draft, derived while <see cref="RoadBuilderPhase.Connecting"/>.
        /// </summary>
        public LaneMapping? Mapping { get; set; }

        /// <summary>The lane under the cursor, if any.</summary>
        public LaneId? HoveredLane { get; set; }

        /// <summary>The lane being dragged, if any.</summary>
        public LaneId? DraggedLane { get; set; }

        /// <summary>The insertion point a dragged lane would be dropped at.</summary>
        public int DropIndex { get; set; }

        /// <summary>Whether the draft is mirrored left-to-right.</summary>
        public bool Mirror { get; set; }

        /// <summary>Whether a commit applies to both ends of the node.</summary>
        public bool ApplyToBothEnds { get; set; }

        /// <summary>True when a draft exists and can be edited.</summary>
        public bool HasDraft => Draft != null;

        /// <summary>
        /// The destination coordinates
        /// </summary>
        public Vector3 DestinationPosition { get; set; }

        /// <summary>
        /// Starts a new draft from a source half-node, copying that end's lanes in its own left-to-right
        /// order. This is the <c>Idle → SourcePicked</c> transition.
        /// </summary>
        public void BeginFrom(HalfNode sourceHalfNode) {
            ArgumentNullException.ThrowIfNull(sourceHalfNode, nameof(sourceHalfNode));
            SourceHalfNode = sourceHalfNode;
            SourceDraft = NodeSpecDraft.FromHalfNode(sourceHalfNode);
            Draft = SourceDraft.Clone();
            Mapping = null;
            HoveredLane = null;
            DraggedLane = null;
            DropIndex = 0;
            Mirror = false;
            Phase = RoadBuilderPhase.SourcePicked;
        }

        /// <summary>
        /// Re-derives the mapping between the source and the draft. Called while
        /// <see cref="RoadBuilderPhase.Connecting"/> so the connectors stay in step with edits.
        /// </summary>
        public void RefreshMapping() {
            if (SourceDraft == null || Draft == null) {
                Mapping = null;
                return;
            }
            Mapping = LaneMapping.Derive(SourceDraft, Draft);
        }

        /// <summary>
        /// Steps back one phase, following the <c>Esc / right-click</c> transition. Returns to
        /// <see cref="RoadBuilderPhase.Idle"/> from <see cref="RoadBuilderPhase.SourcePicked"/>, which
        /// discards the draft.
        /// </summary>
        public void StepBack() {
            switch (Phase) {
                case RoadBuilderPhase.Connecting:
                case RoadBuilderPhase.Placing:
                    Phase = RoadBuilderPhase.Dragging;
                    break;
                case RoadBuilderPhase.Dragging:
                    Phase = RoadBuilderPhase.SourcePicked;
                    break;
                case RoadBuilderPhase.SourcePicked:
                    Reset();
                    break;
            }
        }

        //Editing. Every operation goes through here so that the renderer is invalidated and the mapping
        //refreshed in one place, rather than at each call site.

        /// <summary>
        /// Raised after any edit, so the renderer can invalidate its cached preview node.
        /// </summary>
        public event Action? Edited;

        /// <summary>Notifies listeners that the draft changed.</summary>
        private void FireEdited() {
            Edited?.Invoke();
            if (Phase == RoadBuilderPhase.Connecting) RefreshMapping();
        }

        /// <summary>Inserts a lane at <paramref name="index"/>, 0 being leftmost.</summary>
        public LaneId InsertLane(int index, LaneSpec spec) {
            var id = RequireDraft().Insert(index, spec);
            FireEdited();
            return id;
        }

        /// <summary>Removes a lane.</summary>
        public bool RemoveLane(LaneId id) {
            var removed = RequireDraft().Remove(id);
            if (removed) FireEdited();
            return removed;
        }

        /// <summary>Moves a lane to a new index, reordering the cross-section.</summary>
        public void MoveLane(LaneId id, int newIndex) {
            RequireDraft().Move(id, newIndex);
            FireEdited();
        }

        /// <summary>Replaces a lane's specification.</summary>
        public void SetLaneSpec(LaneId id, LaneSpec spec) {
            RequireDraft().SetSpec(id, spec);
            FireEdited();
        }

        /// <summary>Widens or narrows a lane, pushing its neighbours outward.</summary>
        public void SetLaneWidth(LaneId id, float width) {
            var draft = RequireDraft();
            var spec = draft.Get(id).Spec;
            spec.Width = width;
            draft.SetSpec(id, spec);
            FireEdited();
        }

        /// <summary>Flips a lane's direction (§5.4).</summary>
        public void ToggleDirection(LaneId id) {
            RequireDraft().ToggleDirection(id);
            FireEdited();
        }

        /// <summary>Inserts a copy of a lane beside it without moving the original (§5.5).</summary>
        public LaneId ExitLane(LaneId id, int side) {
            var result = RequireDraft().Exit(id, side);
            FireEdited();
            return result;
        }

        /// <summary>Merges the lane at <paramref name="index"/> with the one to its right (§5.2).</summary>
        public LaneId MergeLanes(int index) {
            var result = RequireDraft().Merge(index);
            FireEdited();
            return result;
        }

        /// <summary>Splits the lane at <paramref name="index"/> into two (§5.3).</summary>
        public (LaneId Left, LaneId Right) SplitLane(int index, float fraction = 0.5f) {
            var result = RequireDraft().Split(index, fraction);
            FireEdited();
            return result;
        }

        /// <summary>Reverses the left-to-right order of the lanes.</summary>
        public void MirrorDraft() {
            RequireDraft().Mirror();
            Mirror = !Mirror;
            FireEdited();
        }

        private NodeSpecDraft RequireDraft()
            => Draft ?? throw new InvalidOperationException("There is no draft to edit; pick a source node end first.");

        /// <summary>Discards the draft and returns to <see cref="RoadBuilderPhase.Idle"/>.</summary>
        public void Reset() {
            Phase = RoadBuilderPhase.Idle;
            SourceHalfNode = null;
            Draft = null;
            SourceDraft = null;
            Mapping = null;
            HoveredLane = null;
            DraggedLane = null;
            DropIndex = 0;
            Mirror = false;
        }
    }
}
