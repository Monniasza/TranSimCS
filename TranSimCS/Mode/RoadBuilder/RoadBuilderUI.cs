using System;
using System.Numerics;
using ImGuiNET;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.SilkNet;

namespace TranSimCS.Mode.RoadBuilder {
    /// <summary>
    /// The Road Builder's cross-section strip widget: a horizontal band showing every lane of the draft
    /// to scale, with the selected lane highlighted and the gaps between lanes clickable.
    /// <para>
    /// This is the primary editing surface. It replaces the old nested-menu lane editor, which could only
    /// ever address the outermost lane. Because the strip is drawn from the draft's derived offsets, it
    /// shows the real cross-section: widening a lane visibly pushes its neighbours outward.
    /// </para>
    /// </summary>
    public static class RoadBuilderUI {
        /// <summary>Pixels per metre in the strip widget.</summary>
        private const float PixelsPerMeter = 24f;

        /// <summary>Height of the strip widget in pixels.</summary>
        private const float StripHeight = 64f;

        /// <summary>Width of the clickable gap between lanes, in pixels.</summary>
        private const float GapWidth = 8f;

        /// <summary>
        /// Draws the cross-section strip. Returns the lane the user clicked, or <see langword="null"/>.
        /// </summary>
        /// <param name="state">The tool state; the draft is read and edited through it.</param>
        /// <param name="selected">The currently selected lane, or <see langword="null"/>.</param>
        /// <param name="insertAt">
        /// Set to the index the user clicked a gap at, or <see langword="null"/> if no gap was clicked.
        /// </param>
        public static LaneId? DrawStrip(RoadBuilderState state, LaneId? selected, out int? insertAt) {
            insertAt = null;
            var draft = state.Draft;
            if (draft == null || draft.Count == 0) {
                ImGui.TextDisabled("No lanes. Use the buttons below to add one.");
                return null;
            }

            var bounds = draft.ComputeBounds();
            var totalWidth = draft.Range.Width();
            var stripWidth = MathF.Max(totalWidth * PixelsPerMeter, 64f);

            var origin = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();
            var clicked = ImGui.InvisibleButton("##cross-section", new Vector2(stripWidth, StripHeight));
            var hovered = ImGui.IsItemHovered();
            var mouse = ImGui.GetIO().MousePos;

            //Map a cross-section coordinate to a pixel offset within the strip.
            float ToPixel(float offset) => (offset - draft.Range.Min) * PixelsPerMeter;

            //Draw each lane as a filled rectangle, labelled with its vehicle types.
            for (int i = 0; i < draft.Count; i++) {
                var lane = draft[i];
                var left = origin.X + ToPixel(bounds[i].Min);
                var right = origin.X + ToPixel(bounds[i].Max);
                var isSelected = selected is LaneId sel && sel == lane.Id;

                var color = lane.Spec.Color.ToVector4();
                var fill = ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, 0.85f));
                drawList.AddRectFilled(new Vector2(left, origin.Y), new Vector2(right, origin.Y + StripHeight), fill);

                var border = isSelected ? new Vector4(1, 1, 0, 1) : new Vector4(0, 0, 0, 0.6f);
                drawList.AddRect(new Vector2(left, origin.Y), new Vector2(right, origin.Y + StripHeight),
                    ImGui.GetColorU32(border), 0, ImDrawFlags.None, isSelected ? 3f : 1f);

                //Direction arrow: a reversed lane points the other way.
                var reversed = lane.Spec.Flags.HasFlag(LaneFlags.IsMerge);
                DrawDirectionArrow(drawList, left, right, origin.Y + StripHeight / 2, reversed);

                //Label the lane with its width, when there is room.
                if (right - left > 28) {
                    var label = $"{lane.Spec.Width:0.#}";
                    var size = ImGui.CalcTextSize(label);
                    drawList.AddText(new Vector2((left + right) / 2 - size.X / 2, origin.Y + 4),
                        ImGui.GetColorU32(new Vector4(1, 1, 1, 0.9f)), label);
                }
            }

            //Draw the clickable gaps between lanes.
            for (int i = 0; i <= draft.Count; i++) {
                var gapCenter = GapCenter(bounds, i);
                var x = origin.X + ToPixel(gapCenter);
                var rectMin = new Vector2(x - GapWidth / 2, origin.Y);
                var rectMax = new Vector2(x + GapWidth / 2, origin.Y + StripHeight);

                var gapHovered = hovered && mouse.X >= rectMin.X && mouse.X <= rectMax.X;
                if (gapHovered)
                    drawList.AddRectFilled(rectMin, rectMax, ImGui.GetColorU32(new Vector4(0.3f, 1f, 0.3f, 0.5f)));

                if (gapHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left)) insertAt = i;
            }

            //Report a lane click, unless a gap took it.
            if (clicked && insertAt == null) {
                for (int i = 0; i < draft.Count; i++) {
                    var left = origin.X + ToPixel(bounds[i].Min);
                    var right = origin.X + ToPixel(bounds[i].Max);
                    if (mouse.X >= left && mouse.X <= right) return draft[i].Id;
                }
            }
            return null;
        }

        /// <summary>
        /// The cross-section coordinate of the gap at <paramref name="index"/>, matching the pickable
        /// regions the renderer publishes.
        /// </summary>
        private static float GapCenter(Geometry.Interval<float>[] bounds, int index) {
            if (index == 0) return bounds[0].Min;
            if (index == bounds.Length) return bounds[^1].Max;
            return (bounds[index - 1].Max + bounds[index].Min) / 2;
        }

        /// <summary>
        /// Draws a small arrow showing which way traffic flows in the lane.
        /// </summary>
        private static void DrawDirectionArrow(ImDrawListPtr drawList, float left, float right, float y, bool reversed) {
            var center = (left + right) / 2;
            var half = MathF.Min((right - left) / 4, 8f);
            if (half < 3) return;

            var color = ImGui.GetColorU32(new Vector4(1, 1, 1, 0.9f));
            var tip = reversed ? center - half : center + half;
            var tail = reversed ? center + half : center - half;
            drawList.AddLine(new Vector2(tail, y), new Vector2(tip, y), color, 2f);
            drawList.AddTriangleFilled(
                new Vector2(tip, y),
                new Vector2(tip + (reversed ? half / 2 : -half / 2), y - half / 2),
                new Vector2(tip + (reversed ? half / 2 : -half / 2), y + half / 2),
                color);
        }

        /// <summary>
        /// Draws the per-lane inspector for the selected lane, wired to
        /// <see cref="DearUI.InputLaneSpec"/>. Returns <see langword="true"/> if anything changed.
        /// </summary>
        public static bool DrawLaneInspector(RoadBuilderState state, LaneId? selected) {
            if (selected is not LaneId id || state.Draft == null || !state.Draft.Contains(id)) {
                ImGui.TextDisabled("Select a lane to edit it.");
                return false;
            }

            var draft = state.Draft;
            var index = draft.IndexOf(id);
            var spec = draft.Get(id).Spec;
            var changed = false;

            ImGui.Text($"Lane {index + 1} of {draft.Count}");

            //The spec editor already handles colour, width, speed, line width, vehicle types and flags.
            if (DearUI.InputLaneSpec("Lane specification", ref spec)) {
                state.SetLaneSpec(id, spec);
                changed = true;
            }

            ImGui.Separator();

            //Direction toggle (§5.4).
            var reversed = spec.Flags.HasFlag(LaneFlags.IsMerge);
            if (ImGui.Button(reversed ? "Direction: reversed" : "Direction: forward"))
                state.ToggleDirection(id);

            //Exit (§5.5): insert a copy beside this lane without moving it.
            if (ImGui.Button("Exit left")) state.ExitLane(id, -1);
            ImGui.SameLine();
            if (ImGui.Button("Exit right")) state.ExitLane(id, 1);

            //Merge with the lane to the right (§5.2).
            if (index + 1 < draft.Count) {
                if (ImGui.Button("Merge with right")) state.MergeLanes(index);
            } else {
                ImGui.BeginDisabled();
                ImGui.Button("Merge with right");
                ImGui.EndDisabled();
            }

            ImGui.SameLine();
            if (ImGui.Button("Split")) state.SplitLane(index);

            //Reorder.
            ImGui.BeginDisabled(index == 0);
            if (ImGui.Button("Move left")) state.MoveLane(id, index - 1);
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.BeginDisabled(index + 1 >= draft.Count);
            if (ImGui.Button("Move right")) state.MoveLane(id, index + 1);
            ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button("Delete")) state.RemoveLane(id);

            return changed;
        }
    }
}
