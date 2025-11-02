using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Menus.InGame;
using TranSimCS.Tools.Panels;

namespace TranSimCS.Tools {
    /// <summary>
    /// Dumps random roads onto the world
    /// </summary>
    public class DumpingTool : ITool {
        public DumpingTool(InGameMenu menu) {
            this.game = menu;
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

        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showDumpTools);
        }

        
        private InGameMenu game;
    }
}
