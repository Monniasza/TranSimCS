using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TranSimCS.Menus.InGame;

namespace TranSimCS.Tools {
    public class ToolEquidistant(InGameMenu menu) : ITool {
        public string Name => "Equdistant Nodes & Segments Tool";

        public string Description => "Pick node[s] to make equidistant path(s)";

        public void Draw(GameTime gameTime) {
            //unused
        }

        public void Draw2D(GameTime gameTime) {
            //unused
        }


        private (object[], string)[] RoadBuildProps = {
            ([Keys.J], "Move left"), ([Keys.L], "Move right"),
            ([Keys.U], "From the left"), ([Keys.O], "From the right"),
            ([Keys.I], "Up"), ([Keys.K], "Down"),
            ([Keys.P], "Forward"), ([Keys.OemSemicolon], "Backward")
        };  
        public (object[], string)[] PromptKeys() {
            return RoadBuildProps;
        }

        public void Update(GameTime gameTime) {
            //unused
        }

        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showPosManip);
        }
    }
}
