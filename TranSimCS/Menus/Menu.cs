using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Ui;

namespace TranSimCS.Menus {
    public abstract class Menu {
        public Menu(Game1 game) {
            Game = game;
            UiSystem = new UiSystem(game, game.defaultUiStyle, game.ih);
        }
        public abstract void Update(GameTime time);
        public abstract void Destroy();
        public abstract void Draw(GameTime time);
        public abstract void LoadContent();

        //UI system
        public Game1 Game { get; private set; }
        public UiSystem UiSystem { get; private set; }
    }
}
