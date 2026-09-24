using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using LanguageExt;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Mode.RoadBuilder;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.SilkNet;

namespace TranSimCS.Mode.NodeEditor {
    public static class NodeEditorUI {
        /// <summary>Pixels per metre in the strip widget.</summary>
        private const float PixelsPerMeter = 24f;

        /// <summary>Height of the strip widget in pixels.</summary>
        private const float StripHeight = 64f;

        /// <summary>Width of the clickable gap between lanes, in pixels.</summary>
        private const float GapWidth = 8f;

        /// <summary>
        /// Draws the cross-section strip. Returns the lane the user clicked, or <see langword="null"/>.
        /// </summary>
        /// <param name="selected">The currently selected lane, or <see langword="null"/>.</param>
        /// <param name="clipboard">Stores the menu's copied lane spec</param>
        /// <param name="node">The currently edited road node</param>
        public static void ShowNodeEditor(HalfNode node, ref HalfLane selected, ref LaneSpec clipboard) {
            ImGui.Text("Road Node Editor");

            var totalWidth = node.Bounds.Width();
            var stripWidth = MathF.Max(totalWidth * PixelsPerMeter, 64f);
            var drawList = ImGui.GetForegroundDrawList();
            var origin = ImGui.GetCursorScreenPos();
            var clicked = ImGui.InvisibleButton("##cross-section", new Vector2(stripWidth, StripHeight));
            var hovered = ImGui.IsItemHovered();
            var mouse = ImGui.GetIO().MousePos;

            bool InRectangle(Vector2 min, Vector2 max) =>
                mouse.X >= min.X && mouse.X <= max.X &&
                mouse.Y >= min.Y && mouse.Y <= max.Y;

            //Map a cross-section coordinate to a pixel offset within the strip.
            float ToPixel(float offset) => (offset - node.Bounds.Min) * PixelsPerMeter;


            for (int i = 0; i < node.LaneCount; i++) {
                var lane = node.GetLaneByIndex(i);
                var isSelected = lane == selected;
                var left = origin.X + ToPixel(lane.Bounds.Min);
                var right = origin.X + ToPixel(lane.Bounds.Max);
                var borderColor = isSelected ? new Vector4(1, 1, 0, 1) : new Vector4(0, 0, 0, 0.6f);
                var min = new Vector2(left, origin.Y);
                var max = new Vector2(right, origin.Y + StripHeight);

                var color = lane.LaneSpec.Color.ToVector4();
                var fill = ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, 0.85f));
                drawList.AddRectFilled(min, max, fill);
                drawList.AddRect(min, max,
                    ImGui.GetColorU32(borderColor), 0, ImDrawFlags.None, isSelected ? 3f : 1f);

                //Label the lane with its width, when there is room.
                if (right - left > 28) {
                    var label = $"{lane.LaneSpec.Width:0.#}";
                    var size = ImGui.CalcTextSize(label);
                    drawList.AddText(new Vector2((left + right) / 2 - size.X / 2, origin.Y + 4),
                        ImGui.GetColorU32(new Vector4(1, 1, 1, 0.9f)), label);
                }

                if (clicked && InRectangle(min, max)) {
                    //Lane picked
                    selected = lane;
                }
            }

            DrawLaneInspector(node, ref selected, ref clipboard);
            if (selected == null) return;

            ImGui.SeparatorText("Lane spec clipboard");
            if (ImGui.BeginChild("###laneeditorclip")) {
                DearUI.InputLaneSpec("", ref clipboard);
                ImGui.EndChild();
            }
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
        public static bool DrawLaneInspector(HalfNode node, ref HalfLane? lane, ref LaneSpec clipboard) {
            if (lane == null || lane.HalfNode != node) {
                ImGui.TextDisabled("Select a lane to edit it.");
                return false;
            }

            var spec = lane.LaneSpec;
            var changed = false;
            var index = lane.Index;

            ImGui.Text($"Lane {index + 1} of {node.LaneCount}");

            //The spec editor already handles colour, width, speed, line width, vehicle types and flags.
            if (DearUI.InputLaneSpec("Lane specification", ref spec)) {
                lane.LaneSpec = spec;
                changed = true;
            }

            ImGui.Separator();

            if (ImGui.Button("Copy")) clipboard = lane.LaneSpec;

            ImGui.SameLine();
            if (ImGui.Button("Paste")) lane.LaneSpec = clipboard;

            ImGui.SameLine();
            if (ImGui.Button("Delete")) {
                node.Delete(lane);
                lane = null;
                return true;
            }

            if (ImGui.Button("Insert a lane on the left, shift medians")) HalfNodeMethods.InsertOnLeft(lane, clipboard);
            ImGui.SameLine();
            if (ImGui.Button("Insert a lane on the right, shift medians")) HalfNodeMethods.InsertOnRight(lane, clipboard);

            if (ImGui.Button("Insert a lane on the left, cut medians")) HalfNodeMethods.InsertOnLeft(lane, clipboard, true);
            ImGui.SameLine();
            if (ImGui.Button("Insert a lane on the right, cut medians")) HalfNodeMethods.InsertOnRight(lane, clipboard, true);

            if (ImGui.Button("Insert a space on the left")) HalfNodeMethods.InsertSpaceOnLeft(lane, clipboard.Width);
            ImGui.SameLine();
            if (ImGui.Button("Insert a space on the right")) HalfNodeMethods.InsertSpaceOnRight(lane, clipboard.Width);

            if (ImGui.Button("Pull lanes on the left towards the selection")) HalfNodeMethods.InsertSpaceOnLeft(lane, -clipboard.Width);
            ImGui.SameLine();
            if (ImGui.Button("Pull lanes on the right towards the selection")) HalfNodeMethods.InsertSpaceOnRight(lane, -clipboard.Width);

            EditHalfLaneBorders(lane);

            
            return changed;
        }

        public static void EditHalfLaneBorders(HalfLane lane) {
            float lpos = lane.Bounds.Min;
            float cpos = lane.MiddlePosition;
            float rpos = lane.Bounds.Max;
            float width = lane.Width;

            bool dimensionsChanged = false;
            if (ImGui.DragFloat("Move: L", ref lpos, 0.005f, rpos - 10, rpos)) {
                if (lpos > rpos) lpos = rpos;
                cpos = (lpos + rpos) / 2;
                width = rpos - lpos;
                dimensionsChanged = true;
            }
            if (ImGui.DragFloat("C", ref cpos, 0.01f, -100, 100)) {
                float hwidth = width / 2l;
                lpos = cpos - hwidth;
                rpos = cpos + hwidth;
                dimensionsChanged = true;
            }
            if (ImGui.DragFloat("R", ref rpos, 0.005f, lpos, lpos + 10)) {
                if (lpos > rpos) rpos = lpos;
                cpos = (lpos + rpos) / 2;
                width = rpos - lpos;
                dimensionsChanged = true;
            }
            if (dimensionsChanged) {
                lane.Bounds = new(lpos, rpos);
            }
        }
    }
}
