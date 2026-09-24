using ImGuiNET;
using Silk.NET.Input;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TranSimCS.Model;
using TranSimCS.Mode.RoadConstruction;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Select;
using TranSimCS.SilkNet;
using TranSimCS.SilkNet.RoadConstruction;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Mode {
    /// <summary>
    /// Splits a road strip at a single position. Click a half-lane to set a reference (which
    /// identifies the road strip(s) attached to that lane end), then click a road strip attached
    /// to the reference to split it at the cursor position. Right-click cancels the reference.
    /// </summary>
    public class ModeSplitAt: IMode {
        public SilkNetTest Menu { get; }
        public HalfLane? ReferenceHalfLane { get; private set; }
        public RoadStrip? TargetRoad { get; private set; }
        public float SplitT { get; private set; }
        private List<RoadStrip> _candidates = new();

        public ModeSplitAt(SilkNetTest menu) {
            Menu = menu;
        }
        string IMode.Title() => "Split in 1 point";

        void IMode.DrawUI() {
            Vector4 red = new(1, 0, 0, 1);
            Vector4 green = new(0, 1, 0, 1);
            Vector4 maroon = new(0.5f, 0, 0, 1);
            Vector4 yellow = new(1, 1, 0, 1);
            if (ImGui.Begin("Split road at point")) {
                var reference = ReferenceHalfLane;
                if (reference is null) {
                    var hovered = Menu.MouseOver?.As<HalfLane>();
                    if (hovered is not null)
                        ImGui.TextColored(green, "[LMB] over a half-lane to set it as the reference. The strip between the reference and selection will be one-to-one. ");
                    else
                        ImGui.TextColored(maroon, "Hover a half-lane to set the reference");
                } else {
                    ImGui.TextColored(yellow, $"Reference half-lane {reference.Guid}");
                    if (_candidates.Count == 0)
                        ImGui.TextColored(red, "No road strips attached to this half-lane. [RMB] to cancel");
                    else if (TargetRoad is not null) {
                        if (SplitT > 0 && SplitT < 1)
                            ImGui.TextColored(green, $"[LMB] to split at t={SplitT:F3}. The strip between the reference and selection will be one-to-one. [RMB] to cancel");
                        else
                            ImGui.TextColored(red, "Cursor is at a road end; move it along the strip. [RMB] to cancel");
                    } else
                        ImGui.TextColored(maroon, "Hover a road strip attached to the reference. [RMB] to cancel");
                }
                ImGui.End();
            }
        }

        void IMode.Draw3D(RenderTarget target, MultiMesh renderMeshPool) {
            var reference = ReferenceHalfLane;
            if (reference is null) return;

            float yoffset2 = 0.5f;
            var renderBin = renderMeshPool.GetOrCreateRenderBinForced(Materials.WhiteTransparent);

            void DrawTickMark(OrthodistantBasis basis, float t, float hlength, float width, Color c) {
                var sample = basis.SampleFrame(t);
                var p0 = sample.O - sample.X * hlength + yoffset2 * sample.Y;
                var p1 = sample.O + sample.X * hlength + yoffset2 * sample.Y;
                renderBin.DrawLine(p0, p1, sample.Y, c, width);
            }

            //Draw the candidate road strips attached to the reference half-lane
            foreach (var road in _candidates)
                SplitRoadMethods.DrawRoadSpline(road, renderBin, Colors.Cyan);

            //Draw the split position on the targeted road strip
            var targetRoad = TargetRoad;
            if (targetRoad is not null && SplitT > 0 && SplitT < 1)
                DrawTickMark(targetRoad.ToolBasis, SplitT, 2, 1, Colors.SkyBlue);

            //Mark the reference half-lane
            var frame = reference.HalfNode.Cache.ReferenceFrame;
            var refPos = frame.O + frame.X * reference.MiddlePosition + frame.Y * yoffset2;
            renderBin.DrawLine(refPos - frame.X, refPos + frame.X, frame.Y, Colors.Yellow, 1);
        }

        void IMode.Update(double dt) {
            TargetRoad = null;
            SplitT = 0;

            var reference = ReferenceHalfLane;
            _candidates = reference is null ? new() : reference.RoadNode.Connections.ToList();
            if (reference is null || _candidates.Count == 0) return;

            var selection = Menu.MouseOver;
            if (!selection.HasValue) return;
            var hoveredRoad = selection.Value.As<RoadStrip>();
            if (hoveredRoad is null || !_candidates.Contains(hoveredRoad)) return;

            TargetRoad = hoveredRoad;
            SplitT = hoveredRoad.ToolBasis.UnTransform(selection.Value.Coordinates).Z;
        }

        CursorType IMode.GetCursor() {
            if (ReferenceHalfLane == null)
                return Menu.MouseOver?.As<HalfLane>() != null ? CursorType.Open : CursorType.Default;
            if (_candidates.Count == 0) return CursorType.Unavailable;
            if (TargetRoad == null) return CursorType.Default;
            return SplitT > 0 && SplitT < 1 ? CursorType.Add : CursorType.Unavailable;
        }

        void IMode.OnMousePress(MouseButton button) {
            if (button == MouseButton.Right) {
                ReferenceHalfLane = null;
                return;
            }
            if (button != MouseButton.Left) return;

            if (ReferenceHalfLane is null) {
                var picked = Menu.MouseOver?.As<HalfLane>();
                if (picked is not null) ReferenceHalfLane = picked;
            } else {
                var road = TargetRoad;
                if (road is not null && SplitT > 0 && SplitT < 1) {
                    var isLaneStart = road.StartNode == ReferenceHalfLane.HalfNode;

                    var (startT, endT) = isLaneStart ? (SplitT, 1f) : (0f, SplitT);

                    //Split at one position: minT=0 produces two segments ([0..t] and [t..1])
                    SplitRoad.SplitSegment(road, startT, endT);
                    Menu.MouseOver = null;
                    ReferenceHalfLane = null;
                }
            }
        }

        void IMode.WorldChanged(TSWorld world) {
            ReferenceHalfLane = null;
            TargetRoad = null;
            SplitT = 0;
            _candidates = new();
        }
    }
}
