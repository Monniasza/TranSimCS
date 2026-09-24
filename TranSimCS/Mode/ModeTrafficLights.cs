using System;
using System.Collections.Immutable;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Section;
using TranSimCS.Roads.Strip;
using TranSimCS.Select;
using TranSimCS.SilkNet;
using TranSimCS.TrafficLights;
using TranSimCS.Worlds;

namespace TranSimCS.Mode {
    /// <summary>
    /// The traffic light editor. Traffic light groups are attached to road sections: each section can hold
    /// at most one group, but a group can control several sections.
    /// Click on a road section to create/select its traffic light group. While a group is being edited,
    /// click on other sections to attach/detach them, and click on individual lights to toggle them for
    /// the current phase.
    /// </summary>
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
                    if (mouseover?.Tag is RoadSection section) {
                        if (section.TrafficLightGroup == null)
                            ImGui.TextColored(green, "[LMB] to create a traffic light group for this road section");
                        else
                            ImGui.TextColored(yellow, "[LMB] to edit this road section's traffic light group");
                    } else if (mouseover?.SelectedObj is TrafficLightGroup) {
                        ImGui.TextColored(yellow, "[LMB] to edit this traffic light group");
                    } else if (mouseover?.Tag != null) {
                        ImGui.TextColored(red, "The selected object does not support traffic lights");
                    } else {
                        ImGui.TextColored(maroon, "No object selected");
                    }
                } else {
                    ImGui.Text("[RMB] to quit editing traffic lights");
                    ImGui.Text("[Q] previous phase, [E] next phase");
                    ImGui.DragFloat("Counter", ref SelectedGroup.Time, 0.1f, 0, float.PositiveInfinity);

                    if (ImGui.Button("Add phase")) AddPhase();
                    ImGui.SameLine();
                    if (SelectedGroup.Phases.Count > 0 && ImGui.Button("Remove current phase")) {
                        SelectedGroup.Phases.RemoveAt(SelectedGroup.PhaseId);
                        if (SelectedGroup.PhaseId >= SelectedGroup.Phases.Count)
                            SelectedGroup.PhaseId = Math.Max(0, SelectedGroup.Phases.Count - 1);
                    }

                    if (SelectedGroup.Phases.Count > 0) {
                        int phaseId = SelectedGroup.PhaseId;
                        if (ImGui.DragInt("Phase", ref phaseId, 0.05f, 0, SelectedGroup.Phases.Count - 1)) {
                            SelectedGroup.PhaseId = Math.Clamp(phaseId, 0, SelectedGroup.Phases.Count - 1);
                            SelectedGroup.Time = 0;
                        }
                            

                        var phase = SelectedGroup.Phases[SelectedGroup.PhaseId];
                        float duration = phase.Duration;
                        if (ImGui.DragFloat("Duration", ref duration, 0.1f, 0.1f, 300f))
                            SelectedGroup.Phases[SelectedGroup.PhaseId] = phase with { Duration = duration };
                    } else {
                        ImGui.TextColored(maroon, "This group has no phases; all lights are permanently green");
                    }

                    if (mouseover?.Tag is RoadSection section) {
                        if (section.TrafficLightGroup == SelectedGroup)
                            ImGui.TextColored(yellow, "[LMB] to detach this road section from the group");
                        else
                            ImGui.TextColored(green, "[LMB] to attach this road section to the group");
                    } else if (mouseover?.Tag is TrafficLight light && light.TrafficLightGroup == SelectedGroup) {
                        var isGreen = SelectedGroup.Phases.Count > 0 && SelectedGroup.CurrentPhase.GreenLanes.Contains(light.lane);
                        ImGui.TextColored(isGreen ? green : red, "[LMB] to toggle this light for the current phase");
                    } else if (mouseover?.Tag is LaneStrip strip && GetControlledLane(strip) is HalfLane hl) {
                        ImGui.TextColored(hl.HasTrafficLight ? yellow : green,
                            hl.HasTrafficLight ? "[LMB] to remove the traffic light from this lane" : "[LMB] to give this lane a traffic light");
                    } else if (mouseover?.Tag != null) {
                        ImGui.TextColored(red, "The selected object does not support traffic lights");
                    } else {
                        ImGui.TextColored(maroon, "No object selected");
                    }
                }
            }
            ImGui.End();
        }

        void IMode.OnMousePress(MouseButton button) {
            var mouseover = game.MouseOver;
            switch (button) {
                case MouseButton.Left:
                    if (SelectedGroup == null) {
                        if (mouseover?.Tag is RoadSection section) {
                            var group = section.TrafficLightGroup;
                            if (group == null) {
                                group = new TrafficLightGroup();
                                section.TrafficLightGroup = group;
                                game.World.TrafficLights.data.Add(group);
                                foreach(var node in section.Nodes) foreach(var lane in node.OppositeHalf.SortedLanes) {
                                    var hasIncomingLanes = lane.HasIncomingLaneStrip;
                                    if(hasIncomingLanes) lane.HasTrafficLight = true;
                                }
                            }
                            SelectedGroup = group;
                        } else if (mouseover?.SelectedObj is TrafficLightGroup existingGroup) {
                            SelectedGroup = existingGroup;
                        }
                    } else {
                        if (mouseover?.Tag is RoadSection section) {
                            section.TrafficLightGroup = section.TrafficLightGroup == SelectedGroup ? null : SelectedGroup;
                            if(SelectedGroup.ControlledSections.Count == 0) {
                                //Removed all road sections. Delete.
                                SelectedGroup = null;
                            }
                        } else if (mouseover?.Tag is TrafficLight light && light.TrafficLightGroup == SelectedGroup) {
                            ToggleLight(light.lane);
                        } else if (mouseover?.Tag is LaneStrip strip && GetControlledLane(strip) is HalfLane hl) {
                            hl.HasTrafficLight = !hl.HasTrafficLight;
                        }
                    }
                    break;
                case MouseButton.Right:
                    SelectedGroup = null;
                    break;
            }
        }

        CursorType IMode.GetCursor() {
            var mouseover = game.MouseOver;
            var tag = mouseover?.Tag;
            if (SelectedGroup == null) {
                if (tag is RoadSection section)
                    return section.TrafficLightGroup == null ? CursorType.Add : CursorType.Open;
                if (mouseover?.SelectedObj is TrafficLightGroup) return CursorType.Open;
                if (tag != null) return CursorType.Unavailable;
                return CursorType.Default;
            }
            if (tag is RoadSection sec)
                return sec.TrafficLightGroup == SelectedGroup ? CursorType.Remove : CursorType.Add;
            if (tag is TrafficLight light && light.TrafficLightGroup == SelectedGroup)
                return CursorType.Open;
            if (tag is LaneStrip strip && GetControlledLane(strip) is HalfLane hl)
                return hl.HasTrafficLight ? CursorType.Remove : CursorType.Add;
            if (tag != null) return CursorType.Unavailable;
            return CursorType.Default;
        }

        // Given a lane strip under the mouse, finds which of its two ends is the half-lane entering
        // a road section controlled by the currently selected group (if any), so it can be given a light.
        private HalfLane? GetControlledLane(LaneStrip strip) {
            if (SelectedGroup == null) return null;
            if (strip.StartLane.TrafficLight == SelectedGroup) return strip.StartLane;
            if (strip.EndLane.TrafficLight == SelectedGroup) return strip.EndLane;
            return null;
        }

        private void ToggleLight(HalfLane lane) {
            if (SelectedGroup == null) return;
            if (SelectedGroup.Phases.Count == 0) AddPhase();

            var phaseId = SelectedGroup.PhaseId;
            var phase = SelectedGroup.Phases[phaseId];
            var greenLanes = phase.GreenLanes.Contains(lane) ? phase.GreenLanes.Remove(lane) : phase.GreenLanes.Add(lane);
            SelectedGroup.Phases[phaseId] = phase with { GreenLanes = greenLanes };
        }
        private void AddPhase() {
            SelectedGroup.Phases.Add(new TrafficLightPhase(30, ImmutableHashSet<HalfLane>.Empty));
            if (SelectedGroup.PhaseId == SelectedGroup.Phases.Count - 2) SelectedGroup.PhaseId++;
        }

        void IMode.OnKeyPress(Key key) {
            if (SelectedGroup == null || SelectedGroup.Phases.Count == 0) return;
            if (key == Key.Q) SelectedGroup.PhaseId = Math.Max(0, SelectedGroup.PhaseId - 1);
            if (key == Key.E) SelectedGroup.PhaseId = Math.Min(SelectedGroup.Phases.Count - 1, SelectedGroup.PhaseId + 1);
        }

        void IMode.WorldChanged(TSWorld world) {
            SelectedGroup = null;
        }
    }
}
