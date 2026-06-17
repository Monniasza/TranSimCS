using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Menus.InGame;

namespace TranSimCS.Tools {
    public class LaneSpecTool(InGameMenu game) : ITool {
        string ITool.Name => "Paint and pick lane specs";

        string ITool.Description => "Click on roads to set their lane specs";

        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left], " on roads to set their lane specs"),
            ([MouseButton.Right], " near a node to select its lane spec"),
            ([MouseButton.Right], " in the middle of a lane strip for the lane strip's spec")
        ];

        void ITool.Draw(GameTime gameTime) {
            //unused
        }

        void ITool.Draw2D(GameTime gameTime) {
            //unused
        }

        void ITool.OnClick(MouseButton button) {
            if(button == MouseButton.Left) {
                var laneSpec = game.configuration.LaneSpec;
                var selection = game.MouseOver;
                var lane = selection?.GetLane();
                if(lane != null) lane.Spec = laneSpec;
                var strip = selection?.GetLaneStrip();
                if (strip != null) strip.Spec = laneSpec;
            }
            if (button == MouseButton.Right) {
                var selection = game.MouseOver;
                var laneSpec = selection?.GetLaneStrip()?.Spec;
                var nodeSpec = selection?.GetLane()?.Spec;
                var spec = nodeSpec ?? laneSpec;
                if (spec == null) return;
                game.configuration.LaneSpec = spec.Value;
            }
        }

        void ITool.OnKeyDown(Keys key) {
            //unused
        }

        void ITool.OnKeyUp(Keys key) {
            //unused
        }

        void ITool.OnRelease(MouseButton button) {
            //unused
        }

        void ITool.Update(GameTime gameTime) {
            //unused
        }
        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showLaneSpecs);
        }
    }
}
