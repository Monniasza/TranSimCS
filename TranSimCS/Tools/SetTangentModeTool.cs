using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Menus.InGame;

namespace TranSimCS.Tools {
    public class SetTangentModeTool : ITool {
        public readonly InGameMenu menu;
        public string Name => throw new NotImplementedException();

        public string Description => throw new NotImplementedException();

        public SetTangentModeTool(InGameMenu menu) {
            this.menu = menu;
        }

        public void Draw(GameTime gameTime) {
            throw new NotImplementedException();
        }

        public void Draw2D(GameTime gameTime) {
            throw new NotImplementedException();
        }

        public (object[], string)[] PromptKeys() {
            throw new NotImplementedException();
        }

        public void Update(GameTime gameTime) {
            throw new NotImplementedException();
        }
    }
}
