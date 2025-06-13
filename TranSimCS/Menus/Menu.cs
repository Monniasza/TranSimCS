using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Menus {
    public abstract class Menu {
        public abstract void SetGame(Game1 game);
        public abstract void Update(GameTime time);
        public abstract void Destroy();
        public abstract void Draw(GameTime time);
        public abstract void LoadContent();
    }
}
