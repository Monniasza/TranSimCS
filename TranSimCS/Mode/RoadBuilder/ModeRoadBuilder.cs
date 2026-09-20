using System;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Select;
using TranSimCS.SilkNet;
using TranSimCS.Worlds;

namespace TranSimCS.Mode.RoadBuilder {
    /// <summary>
    /// The Road Builder: a tool that edits a <see cref="NodeSpecDraft"/> and commits it to the world.
    /// <para>
    /// This is the Phase 2 skeleton. It establishes the mode, the state machine, the preview renderer and
    /// the <see cref="LaneHit"/> selector; the editing operations, the cross-section strip widget and the
    /// per-lane inspector arrive in Phase 3.
    /// </para>
    /// <para>
    /// <see cref="ModeSegment"/> is deliberately left untouched until the Road Builder reaches parity
    /// (Phase 7).
    /// </para>
    /// </summary>
    public class ModeRoadBuilder : IMode {
        public SilkNetTest Menu { get; private set; }

        /// <summary>The tool state: phase, draft, and transient selection.</summary>
        public RoadBuilderState State { get; private set; } = new();

        /// <summary>The preview renderer, which owns the cached throwaway node.</summary>
        public RoadBuilderRenderer Renderer { get; private set; }

        public ModeRoadBuilder(SilkNetTest menu) {
            Menu = menu;
            Renderer = new RoadBuilderRenderer(menu);
        }

        string IMode.Title() => "Road Builder";

        void IMode.DrawUI() {
            if (!ImGui.Begin("Road Builder")) {
                ImGui.End();
                return;
            }

            ImGui.Text($"Phase: {State.Phase}");
            if (State.SourceEnd != null)
                ImGui.Text($"Source: {State.SourceEnd.Node.Name} ({State.SourceEnd.End})");
            if (State.Draft != null) {
                ImGui.Text($"Lanes: {State.Draft.Count}");
                ImGui.Text($"Width: {State.Draft.TotalWidth:0.##} m");
            }
            if (State.HoveredLane is LaneId hovered)
                ImGui.Text($"Hovered: {hovered}");
            if (State.Mapping != null)
                ImGui.Text($"Mapping: {State.Mapping}");

            ImGui.Separator();
            switch (State.Phase) {
                case RoadBuilderPhase.Idle:
                    ImGui.Text("[LMB] on a node end to start a road");
                    break;
                case RoadBuilderPhase.SourcePicked:
                    ImGui.Text("[LMB] and drag to place the road");
                    ImGui.Text("[RMB] or [Esc] to cancel");
                    break;
                case RoadBuilderPhase.Dragging:
                    ImGui.Text("Release to choose where the road ends");
                    ImGui.Text("[RMB] or [Esc] to go back");
                    break;
                case RoadBuilderPhase.Placing:
                    ImGui.Text("[LMB] to place the road");
                    ImGui.Text("[RMB] or [Esc] to go back");
                    break;
                case RoadBuilderPhase.Connecting:
                    ImGui.Text("[LMB] to connect to the node under the cursor");
                    ImGui.Text("[RMB] or [Esc] to go back");
                    break;
            }

            ImGui.Separator();
            ImGui.Text("[M] to mirror the draft");
            ImGui.Text("[B] to toggle applying to both ends");

            ImGui.End();
        }

        void IMode.OnKeyPress(Key key) {
            switch (key) {
                case Key.Escape:
                    State.StepBack();
                    Renderer.Invalidate();
                    break;
                case Key.M:
                    State.Draft?.Mirror();
                    State.Mirror ^= true;
                    Renderer.Invalidate();
                    break;
                case Key.B:
                    State.ApplyToBothEnds ^= true;
                    break;
            }
        }

        void IMode.OnMousePress(MouseButton button) {
            if (button == MouseButton.Right) {
                State.StepBack();
                Renderer.Invalidate();
                return;
            }
            if (button != MouseButton.Left) return;

            switch (State.Phase) {
                case RoadBuilderPhase.Idle:
                    PickSource();
                    break;
                case RoadBuilderPhase.SourcePicked:
                    State.Phase = RoadBuilderPhase.Dragging;
                    break;
                case RoadBuilderPhase.Placing:
                case RoadBuilderPhase.Connecting:
                    //Committing is Phase 3+ work; for now the skeleton just returns to the start.
                    State.Reset();
                    Renderer.Invalidate();
                    break;
            }
        }

        void IMode.OnMouseRelease(MouseButton button) {
            if (button != MouseButton.Left) return;
            if (State.Phase != RoadBuilderPhase.Dragging) return;

            //Released over an existing node end means connecting; over empty space means placing.
            var overNodeEnd = Menu.MouseOver?.As<RoadNodeEnd>();
            State.Phase = overNodeEnd != null ? RoadBuilderPhase.Connecting : RoadBuilderPhase.Placing;
            if (State.Phase == RoadBuilderPhase.Connecting) State.RefreshMapping();
        }

        /// <summary>
        /// The <c>Idle → SourcePicked</c> transition: pick the node end under the cursor and start a draft
        /// from it.
        /// </summary>
        private void PickSource() {
            var picked = Menu.MouseOver?.As<HalfLane>()?.HalfNode?.RoadNodeEnd;
            if (picked == null) return;
            State.BeginFrom(picked);
            Renderer.Invalidate();
        }

        void IMode.Update(double dt) {
            //Track which lane the cursor is over, so the UI and the editing operations can use it.
            var hit = Menu.MouseOver?.As<LaneHit>();
            State.HoveredLane = hit is LaneHit laneHit && !laneHit.IsInsertion ? laneHit.Id : null;
        }

        void IMode.Draw3D(RenderTarget target, MultiMesh renderMeshPool) {
            Renderer.Draw(State, target, renderMeshPool);
        }

        void IMode.AddSelectors(MultiMesh invisible, MultiMesh visible) {
            Renderer.AddSelectors(State, visible);
        }

        void IMode.WorldChanged(TSWorld world) {
            State.Reset();
            Renderer.Invalidate();
        }
    }
}
