using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Silk.NET.Input;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Section;
using TranSimCS.SilkNet;
using TranSimCS.TrafficLights;

namespace TranSimCS.Mode {
    /// <summary>
    /// The traffic light editor.
    /// Click on sections and 
    /// </summary>
    /// <param name="game"></param>
    public sealed class ModeTrafficLights(SilkNetTest game) : IMode {
        public string Title() => "Traffic lights";

        public TrafficLightGroup? SelectedGroup;

        void IMode.DrawUI() {
            Vector4 red = new(1, 0, 0, 1);
            Vector4 yellow = new(1, 1, 0, 1);
            Vector4 green = new(0, 1, 0, 1);
            Vector4 maroon = new(0.5f, 0, 0, 1);
            if (ImGui.Begin("Traffic light editor")) {
                var mouseover = game.MouseOver;
                if (SelectedGroup == null) {
                    if (mouseover?.SelectedObj == null) {
                        ImGui.TextColored(maroon, "No object selected");
                    } else if (mouseover?.Tag is HalfLane) {
                        ImGui.TextColored(green, "[LMB] to add traffic lights to this lane. [Shift+LMB] to add traffic lights to all lanes on this half node");
                    } else if (mouseover?.Tag is RoadSection) {
                        ImGui.TextColored(green, "[LMB] to add traffic lights to this road section.");
                    } else if (mouseover?.SelectedObj is TrafficLightGroup) {
                        ImGui.TextColored(yellow, "[LMB] to edit this traffic light group.");
                    } else {
                        ImGui.TextColored(red, "The selected object does not support traffic lights.");
                    }
                } else {
                    ImGui.Text("[Q] to go to the previous phase");
                    ImGui.Text("[E] to go to the next phase");
                    ImGui.Text("[RMB] to quit editing traffic lights");
                    ImGui.DragInt($"Phase", ref SelectedGroup.PhaseId, 0.01f, 0, SelectedGroup.Phases.Count - 1);
                    if (mouseover?.SelectedObj == null) {
                        ImGui.TextColored(maroon, "No object selected");
                    } else if (mouseover?.Tag is HalfLane) {
                        ImGui.TextColored(green, "[LMB] to add traffic lights to this lane. [Shift+LMB] to add traffic lights to all lanes on this half node");
                    } else if (mouseover?.Tag is TrafficLight) {
                        ImGui.TextColored(yellow, "[LMB] to toggle this traffic light");
                    } else {
                        ImGui.TextColored(red, "The selected object does not support traffic lights.");
                    }
                }

                
            }
        }

        void IMode.OnMousePress(MouseButton button) {
            bool shiftPressed = ImGui.IsKeyDown(ImGuiKey.LeftShift);
            var mouseover = game.MouseOver?.Tag;
            if(mouseover is HalfLane hlane) {

            }
        }
    }
}
