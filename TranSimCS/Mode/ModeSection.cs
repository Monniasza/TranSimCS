using System.Linq;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Section;
using TranSimCS.Select;
using TranSimCS.SilkNet;

namespace TranSimCS.Mode {
    internal class ModeSection: IMode {
        public readonly SilkNetTest Menu;
        public RoadSection? Section;

        public ModeSection(SilkNetTest world) {
            Menu = world;
        }

        public string Title() => "Create and modify road sections";
        void IMode.DrawUI() {
            Vector4 red = new(1, 0, 0, 1);
            Vector4 yellow = new(1, 1, 0, 1);
            Vector4 green = new(0, 1, 0, 1);
            Vector4 cyan = new(0, 1, 1, 1);
            Vector4 maroon = new(0.5f, 0, 0, 1);
            Vector4 magenta = new(0.5f, 0, 0.5f, 1);
            if (ImGui.Begin("Road sections")) {
                var candidateObject = Menu.MouseOver?.Tag;
                var isSection = candidateObject is RoadSection;
                var isNode = candidateObject is HalfLane;

                if (Section == null) {
                    if (isSection) {
                        ImGui.TextColored(green, "Picked a road section");
                        ImGui.TextColored(yellow, "[LMB] to edit this road section");
                    }else if (isNode) {
                        ImGui.TextColored(green, "Picked a road node");
                        var ownedSection = ((HalfLane)candidateObject).HalfNode.ConnectedSection;
                        if (ownedSection == null)
                            ImGui.TextColored(yellow, "[LMB] to edit attached road section");
                        else
                            ImGui.TextColored(green, "[LMB] to create a road section attached to this road node");
                    } else if(candidateObject != null) {
                        ImGui.TextColored(red, "The selected object can't be a part of a road section");
                    } else {
                        ImGui.TextColored(magenta, "No object selected");
                    }
                } else {
                    ImGui.Text("[RMB] to quit editing current road section");
                    if (isSection) {
                        ImGui.TextColored(green, "Picked a road section");
                        ImGui.TextColored(yellow, "[LMB] to union this road section in");
                    } else if (isNode) {
                        ImGui.TextColored(green, "Picked a road node");
                        var halfnode = ((HalfLane)candidateObject).HalfNode;
                        var ownedSection = halfnode.ConnectedSection.Value;
                        if(ownedSection == Section) {
                            var pair = Section.MainSlopeNodes.Value;
                            if (halfnode == pair.Start) ImGui.TextColored(yellow, "The node is the start of the main slope");
                            if (halfnode == pair.End) ImGui.TextColored(yellow, "The node is the end of the main slope");
                            ImGui.Text("[MMB] to set the node as a start of the main slope");
                            ImGui.Text("[MMB+LCtrl] to set the node as an end of the main slope");
                        } else if (ownedSection == null)
                            ImGui.TextColored(yellow, "[LMB] to switch the road node to the currently edited road section");
                        else {
                            ImGui.TextColored(green, "[LMB] to add the road node to the currently edited road section");
                        }
                            
                    } else if (candidateObject != null) {
                        ImGui.TextColored(red, "The selected object can't be a part of a road section");
                    } else {
                        ImGui.TextColored(magenta, "No object selected");
                    }
                }

                Menu.ShowFinishSettings();
                ImGui.End();
            }
        }

        void IMode.OnMousePress(MouseButton button) {
            if (Section == null && button == MouseButton.Left) {
                var asSection = Menu.MouseOver?.As<RoadSection>();
                if (asSection != null) {
                    Section = asSection;
                    return;
                }

                //Add a section
                var element = Menu.MouseOver?.AsRoadElement();
                if (element == null) return;
                var node = element.GetNodeEnd();
                if (node == null) return;
                Section = node.GetOrCreateSection();
                Section.Finish = Menu.RoadFinish;
                return;
            }
            if (Section != null) {
                var hitObject = Menu.MouseOver?.SelectedObj;
                switch (button) {
                    case MouseButton.Left:
                        //Add/remove a node
                        switch (Menu.MouseOver?.Tag) {
                            case RoadSection section:
                                //Union the road section into the current road section
                                var nodes = section.Nodes.ToArray();
                                foreach (var item in nodes) {
                                    item.ConnectedSection.Value = Section;
                                }
                                break;
                            case HalfLane laneEnd:
                                var nodeEnd = laneEnd.HalfNode;
                                nodeEnd.ConnectedSection.Value = nodeEnd.ConnectedSection.Value == Section ? null : Section;
                                break;
                        }
                        break;
                    case MouseButton.Middle:
                        //Set a slope node
                        if (hitObject is IRoadElement element0) {
                            var node = element0.GetLaneEnd();
                            if (node == null) return;
                            var slopeNodes = Section.MainSlopeNodes.Value;
                            if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl)) {
                                slopeNodes.End = node?.HalfNode;
                            } else {
                                slopeNodes.Start = node?.HalfNode;
                            }
                            Section.MainSlopeNodes.Value = slopeNodes;
                        }
                        break;
                    case MouseButton.Right:
                        Section = null;
                        break;
                }
            }
        }
    }
}
