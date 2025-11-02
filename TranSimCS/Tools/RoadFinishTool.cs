using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Input;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;
using TranSimCS.Tools.Panels;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class RoadFinishTool : ITool {
        private InGameMenu menu;
        public RoadFinishTool(InGameMenu menu) {
            this.menu = menu;
            FinishProp = menu.configuration.RoadFinishProp;
            tab = menu.ToolsPanel.GetPanel<RoadFinishTab>(ToolAttribs.showFinishes);
        }

        public string Name => "Edit road finishes";

        public string Description => "";

        public Property<RoadFinish> FinishProp;
        public RoadFinish Finish { get => FinishProp.Value; set => FinishProp.Value = value; }

        private RoadFinishTab tab;

        public void OnClick(MouseButton button) {
            var selectedRoadStrip = menu.MouseOverRoad?.SelectedLaneTag?.road;
            switch (button) {
                case MouseButton.Left:
                    //Set the finish
                    selectedRoadStrip?.Finish = Finish;
                    break;
                case MouseButton.Right:
                    //Pick a finish
                    if(selectedRoadStrip != null) Finish = selectedRoadStrip.Finish;
                    break;
            }
        }

        public void Draw(GameTime gameTime) {
            //unused
        }

        public void Draw2D(GameTime gameTime) {
            //unused
        }

        public (object[], string)[] PromptKeys() {
            return [
                ([MouseButton.Left], "Apply"),
                ([MouseButton.Right], "Copy")
            ];
        }

        public void Update(GameTime gameTime) {
            //unused
        }

        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showFinishes);
        }
    }
}
