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
using TranSimCS.Roads.Section;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class SecGen: ITool {
        public readonly InGameMenu Menu;
        public RoadSection? section;

        public SecGen(InGameMenu world) {
            Menu = world;
        }

        public string Name => "Create and modify road sections";

        public string Description => "throw new NotImplementedException();";

        public void Draw(GameTime gameTime) {
            //unused
        }

        public void Draw2D(GameTime gameTime) {
            //unused
        }

        public (object[], string)[] PromptKeys() {
            if(section == null) {
                return [
                    ([MouseButton.Left], "Pick a node to start creating a section"),
                    ([MouseButton.Left], "Pick a section to edit it")
                ];
            } else {
                return [
                    ([MouseButton.Left], "Add or remove a road node from/to a section"),
                    ([MouseButton.Right], "Quit editing/finish creation"),
                    ([MouseButton.Middle], "Pick the first slope node"),
                    ([Keys.LeftControl, MouseButton.Middle], "Pick the second slope node"),
                ];
            }
            
        }

        public void Update(GameTime gameTime) {
            //unused
        }
        void ITool.AddAttributes(ISet<string> action) {
            //unused
        }
        void ITool.OnClick(MouseButton button) {
            if (section == null && button == MouseButton.Left) {
                //Add a section
                var hitObject = Menu.SelectedObject;
                if(hitObject is RoadSection s) {
                    section = s;
                }
                if(hitObject is IRoadElement element) {
                    var node = element.GetNodeEnd();
                    if (node == null) return;
                    section = node.GetOrCreateSection();
                }
                return;
            }else if(section != null) {
                var hitObject = Menu.SelectedObject;
                switch (button) {
                    case MouseButton.Left:
                        //Add/remove a node
                        if (hitObject is IRoadElement element) {
                            var node = element.GetNodeEnd();
                            if (node == null) return;
                            node.ConnectedSection.Value = node.ConnectedSection.Value == section ? null : section;
                        }
                        break;
                    case MouseButton.Middle:
                        //Set a slope node
                        if (hitObject is IRoadElement element0) {
                            var node = element0.GetNodeEnd();
                            if (node == null) return;
                            var slopeNodes = section.MainSlopeNodes.Value;
                            if (Menu.Game.KeyboardState.IsKeyDown(Keys.LeftControl)) {
                                slopeNodes.End = node;
                            } else {
                                slopeNodes.Start = node;
                            }
                            section.MainSlopeNodes.Value = slopeNodes;
                        }
                        break;
                    case MouseButton.Right:
                        section = null;
                        break;
                }
            }
        }
    }
}
