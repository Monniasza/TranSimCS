using System.Linq;
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
            ([MouseButton.Right], "on road strips to reverse their direction"),
        ];

        void ITool.OnClick(MouseButton button) {
            if (button == MouseButton.Left) {
                var laneStrip = game.MouseOver?.GetLaneStrip();
                laneStrip?.ReverseDirection();
                game.MouseOver = null;
            }
            if(button == MouseButton.Right) {
                var roadStrip = game.MouseOver?.GetRoadStrip();
                if (roadStrip == null) return;
                var lanes = roadStrip.Lanes.ToArray();
                foreach(var lane in lanes) lane.ReverseDirection();
                game.MouseOver = null;
            }
        }
    }
}