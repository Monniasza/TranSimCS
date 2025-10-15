using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Menus.InGame;

namespace TranSimCS.Tools {
    /// <summary>
    /// Dumps random roads onto the world
    /// </summary>
    public class DumpingTool : ITool {
        public DumpingTool(InGameMenu menu) {
            this.game = menu;
            this.menu = new DumpingMenu(menu);
        }

        public string Name => "Dumping Tool";

        public string Description => "Dump random roads onto the world";

        public void Draw(GameTime gameTime) {
            //unused
        }

        public void Draw2D(GameTime gameTime) {
            //unused
        }

        public (object[], string)[] PromptKeys() {
            return [];
        }

        public void Update(GameTime gameTime) {
            //unused
        }

        
        private InGameMenu game;
        private DumpingMenu menu;
        void ITool.OnOpen() {
            game.UiSystem.Add(RoadCreationTool.uiID, menu);
        }
        void ITool.OnClose() {
            game.UiSystem.Remove(RoadCreationTool.uiID);
        }
    }
}
