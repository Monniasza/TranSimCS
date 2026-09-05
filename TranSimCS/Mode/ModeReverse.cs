using System.Linq;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using TranSimCS.Roads.Strip;
using TranSimCS.Select;
using TranSimCS.SilkNet;

namespace TranSimCS.Mode {
    public class ModeReverse(SilkNetTest game): IMode {
        string IMode.Title() => "Reverse lane direction";
        void IMode.DrawUI() {
            Vector4 red = new(1, 0, 0, 1);
            Vector4 green = new(0, 1, 0, 1);
            Vector4 maroon = new(0.5f, 0, 0, 1);
            if (ImGui.Begin("Reverse")) {
                var selectedLaneStrip = game.MouseOver?.Tag as LaneStrip;
                if (selectedLaneStrip != null)
                    ImGui.TextColored(green, "[LMB] to reverse the lane strip");
                else if (game.MouseOver?.Tag != null)
                    ImGui.TextColored(red, "The selected tag is not reversible");
                else
                    ImGui.TextColored(maroon, "No tag selected");

                var selectedRoadStrip = game.MouseOver?.SelectedObj as RoadStrip;
                if (selectedRoadStrip != null)
                    ImGui.TextColored(green, "[RMB] to reverse all lanes of the road strip");
                else if (game.MouseOver?.SelectedObj != null)
                    ImGui.TextColored(red, "The selected object is not reversible");
                else
                    ImGui.TextColored(maroon, "No object selected");
                
                ImGui.End();
            }
        }

        void IMode.OnMousePress(MouseButton button) {
            if (button == MouseButton.Left) {
                var laneStrip = game.MouseOver?.GetLaneStrip();
                laneStrip?.ReverseDirection();
                game.MouseOver = null;
            }
            if (button == MouseButton.Right) {
                var roadStrip = game.MouseOver?.GetRoadStrip();
                if (roadStrip == null) return;
                var lanes = roadStrip.Lanes.ToArray();
                foreach (var lane in lanes) lane.ReverseDirection();
                game.MouseOver = null;
            }
        }
    }
}
