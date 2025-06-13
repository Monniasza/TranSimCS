using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TranSimCS.Roads;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;

namespace TranSimCS
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Menu menu;

        public Menu Menu { get => menu; set {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            var oldMenu = menu;
            menu?.Destroy();
            menu?.SetGame(null);
            menu = value;
            menu.SetGame(this);
            menu.LoadContent();
            
        } }

        public Game1() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize() {
            // TODO: Add your initialization logic here
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Menu = new InGameMenu();
        }
        
        protected override void Update(GameTime gameTime) {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            Menu?.Update(gameTime);
            //Refresh the mouse state for the next frame
            base.Update(gameTime);
            
        }

        
        protected override void Draw(GameTime gameTime) {
            Menu?.Draw(gameTime);
            base.Draw(gameTime);
        }

        
    }
}