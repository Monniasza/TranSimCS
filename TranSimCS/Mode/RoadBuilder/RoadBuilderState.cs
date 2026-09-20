using System;
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

        /// <summary>The node end the draft is being built from. Null while <see cref="RoadBuilderPhase.Idle"/>.</summary>
        public RoadNodeEnd? SourceEnd { get; set; }

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
        /// Starts a new draft from a source node end, copying that end's lanes. This is the
        /// <c>Idle → SourcePicked</c> transition.
        /// </summary>
        public void BeginFrom(RoadNodeEnd sourceEnd) {
            ArgumentNullException.ThrowIfNull(sourceEnd, nameof(sourceEnd));
            SourceEnd = sourceEnd;
            SourceDraft = NodeSpecDraft.FromHalfNode(sourceEnd.HalfNode);
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

        /// <summary>Discards the draft and returns to <see cref="RoadBuilderPhase.Idle"/>.</summary>
        public void Reset() {
            Phase = RoadBuilderPhase.Idle;
            SourceEnd = null;
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
