using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Section;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class SectionTool: ITool {
        public readonly InGameMenu Menu;
        public RoadSection? Section;

        public SectionTool(InGameMenu world) {
            Menu = world;
        }

        public string Name => "Create and modify road sections";
        public string Description => (Section == null) ? "Pick a node or a section to start editing" : "Editing a road section";

        public (object[], string)[] PromptKeys() {
            if(Section == null) {
                return [
                    ([MouseButton.Left], "Pick a node to start creating a section"),
                    ([MouseButton.Left], "Pick a section to edit it")
                ];
            } else {
                return [
                    ([MouseButton.Left], "on a road not to add or remove a road node from/to a section"),
                    ([MouseButton.Left], "on a road section to union it in"),
                    ([MouseButton.Right], "Quit editing/finish creation"),
                    ([MouseButton.Middle], "Pick the first slope node"),
                    ([Keys.LeftControl, MouseButton.Middle], "Pick the second slope node"),
                ];
            }
            
        }
        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showFinishes);
        }
        void ITool.OnClick(MouseButton button) {
            if (Section == null && button == MouseButton.Left) {
                var asSection = Menu.MouseOver?.As<RoadSection>();
                if(asSection != null) {
                    Section = asSection;
                    return;
                }

                //Add a section
                var element = Menu.MouseOver?.AsRoadElement();
                if (element == null) return;
                var node = element.GetNodeEnd();
                if (node == null) return;
                Section = node.GetOrCreateSection();
                Section.Finish = Menu.configuration.RoadFinish;
                return;
            }
            if(Section != null) {
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
                            case LaneEnd laneEnd:
                                var nodeEnd = laneEnd.RoadNodeEnd;
                                nodeEnd.ConnectedSection.Value = nodeEnd.ConnectedSection.Value == Section ? null : Section;
                                break;
                        }
                        break;
                    case MouseButton.Middle:
                        //Set a slope node
                        if (hitObject is IRoadElement element0) {
                            var node = element0.GetNodeEnd();
                            if (node == null) return;
                            var slopeNodes = Section.MainSlopeNodes.Value;
                            if (Menu.Game.KeyboardState.IsKeyDown(Keys.LeftControl)) {
                                slopeNodes.End = node;
                            } else {
                                slopeNodes.Start = node;
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
