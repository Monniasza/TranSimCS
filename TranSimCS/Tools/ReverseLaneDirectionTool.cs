using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;

namespace TranSimCS.Tools
{
    public class ReverseLaneDirectionTool(InGameMenu game) : ITool
    {
        string ITool.Name => "Reverse lane direction";

        string ITool.Description => "Click on a lane strip to reverse its direction";

        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left], "on lane strips to reverse their direction"),
        ];

        void ITool.Draw(GameTime gameTime) {
        }

        void ITool.Draw2D(GameTime gameTime) {
        }

        void ITool.OnClick(MouseButton button) {
            if (button == MouseButton.Left) {
                var laneStrip = game.MouseOverRoad?.SelectedLaneStrip;
                if (laneStrip != null) {
                    laneStrip.ReverseDirection();
                }
            }
        }

        void ITool.OnKeyDown(Keys key) {
        }

        void ITool.OnKeyUp(Keys key) {
        }

        void ITool.OnRelease(MouseButton button) {
        }

        void ITool.Update(GameTime gameTime) {
        }

        void ITool.AddSelectors(MultiMesh addTo, MultiMesh visibleSelectors) {
        }
    }
}