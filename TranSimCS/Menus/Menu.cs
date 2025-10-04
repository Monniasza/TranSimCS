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
            UiSystem = new UiSystem(game, game.DefaultUiStyle, game.ih);
        }
        public abstract void Update(GameTime time);
        public abstract void Destroy();
        public abstract void Draw(GameTime time);
        public abstract void Draw2D(GameTime time);

        public abstract void LoadContentOverride();

        public bool IsLoaded {get; private set;}
        public void LoadContent() {
            if (IsLoaded) return;
            LoadContentOverride();
            IsLoaded = true;
        }

        //UI system
        public Game1 Game { get; private set; }
        public UiSystem UiSystem { get; private set; }
    }
}
