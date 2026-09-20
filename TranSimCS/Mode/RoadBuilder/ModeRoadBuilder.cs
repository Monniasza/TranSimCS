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

        /// <summary>The lane currently selected for per-lane editing.</summary>
        public LaneId? SelectedLane { get; private set; }

        public ModeRoadBuilder(SilkNetTest menu) {
            Menu = menu;
            Renderer = new RoadBuilderRenderer(menu);
            //Any edit invalidates the cached preview node, so the preview always matches the draft.
            State.Edited += Renderer.Invalidate;
        }

        string IMode.Title() => "Road Builder";

        void IMode.DrawUI() {
            if (!ImGui.Begin("Road Builder")) {
                ImGui.End();
                return;
            }

            ImGui.Text($"Phase: {State.Phase}");
            if (State.SourceHalfNode != null)
                ImGui.Text($"Source: {State.SourceHalfNode.RoadNode.Name} ({State.SourceHalfNode.End})");
            if (State.Draft != null) {
                ImGui.Text($"Lanes: {State.Draft.Count}");
                ImGui.Text($"Width: {State.Draft.TotalWidth:0.##} m");
            }
            if (State.Mapping != null)
                ImGui.Text($"Mapping: {State.Mapping}");

            ImGui.Separator();

            //The cross-section strip is the primary editing surface.
            if (State.Draft != null) {
                var clicked = RoadBuilderUI.DrawStrip(State, SelectedLane, out var insertAt);
                if (clicked is LaneId lane) SelectedLane = lane;
                if (insertAt is int index) {
                    //Insert a lane the same width as the one under the cursor, or a default.
                    var width = SelectedLane is LaneId sel && State.Draft.Contains(sel)
                        ? State.Draft.Get(sel).Spec.Width
                        : 3f;
                    var spec = SelectedLane is LaneId s2 && State.Draft.Contains(s2)
                        ? State.Draft.Get(s2).Spec
                        : new LaneSpec(Colors.Gray, VehicleTypes.Car, width, 50);
                    spec.Width = width;
                    SelectedLane = State.InsertLane(index, spec);
                }

                ImGui.Separator();
                RoadBuilderUI.DrawLaneInspector(State, SelectedLane);

                ImGui.Separator();
                if (ImGui.Button("Add lane at left")) SelectedLane = State.InsertLane(0, DefaultSpec());
                ImGui.SameLine();
                if (ImGui.Button("Add lane at right")) SelectedLane = State.InsertLane(State.Draft.Count, DefaultSpec());
                ImGui.SameLine();
                if (ImGui.Button("Mirror")) State.MirrorDraft();
            }

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

        /// <summary>A default lane spec for a newly added lane.</summary>
        private static LaneSpec DefaultSpec()
            => new(Colors.Gray, VehicleTypes.Car, 3f, 50);

        void IMode.OnKeyPress(Key key) {
            switch (key) {
                case Key.Escape:
                    State.StepBack();
                    Renderer.Invalidate();
                    break;
                case Key.M:
                    if (State.HasDraft) State.MirrorDraft();
                    break;
                case Key.B:
                    State.ApplyToBothEnds ^= true;
                    break;
                case Key.R:
                    //Toggle the selected lane's direction (§5.4).
                    if (SelectedLane is LaneId r && State.HasDraft && State.Draft!.Contains(r))
                        State.ToggleDirection(r);
                    break;
                case Key.E:
                    //Exit the selected lane to the right (§5.5).
                    if (SelectedLane is LaneId e && State.HasDraft && State.Draft!.Contains(e))
                        SelectedLane = State.ExitLane(e, 1);
                    break;
                case Key.Delete:
                    if (SelectedLane is LaneId d && State.HasDraft && State.Draft!.Contains(d)) {
                        State.RemoveLane(d);
                        SelectedLane = null;
                    }
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
            var overNodeEnd = Menu.MouseOver?.As<HalfLane>();
            State.Phase = overNodeEnd != null ? RoadBuilderPhase.Connecting : RoadBuilderPhase.Placing;
            var position = new PositionEulerAngles();
            if (overNodeEnd != null) {
                position = overNodeEnd.HalfNode.PositionData;
                var refframe = overNodeEnd.HalfNode.Cache.ReferenceFrame;
                position.Position += refframe.X * overNodeEnd.MiddlePosition;
            } else {
                var groundPlane = Menu.snappingGrid.CreateSnappingPlane();
                var mouseRay = Menu.MouseRay;
                position.Position = GeometryUtils.IntersectRayPlane(mouseRay, groundPlane);
            }
            State.DestinationPosition = position;
            if (State.Phase == RoadBuilderPhase.Connecting) State.RefreshMapping();
        }

        /// <summary>
        /// The <c>Idle → SourcePicked</c> transition: pick the node end under the cursor and start a draft
        /// from it.
        /// </summary>
        private void PickSource() {
            //Pick through HalfLane, never RoadNodeEnd: RoadNodeEnd is not order-corrected, so indexing
            //lanes through it silently mirrors for the Backward end.
            var picked = Menu.MouseOver?.As<HalfLane>()?.HalfNode;
            if (picked == null) return;
            State.BeginFrom(picked);
            SelectedLane = null;
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
