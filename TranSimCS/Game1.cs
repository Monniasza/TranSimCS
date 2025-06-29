using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Font;
using MLEM.Input;
using MLEM.Ui.Style;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;
using SpriteFontPlus;
using System.IO;

namespace TranSimCS
{
    public class Game1 : Game
    {
        public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }
        public SpriteBatch SpriteBatch { get; private set; }
        private Menu menu;
        public readonly InputHandler ih;
        public UiStyle defaultUiStyle { get; private set; }

        //Fonts
        public SpriteFont Font { get; private set; }
        public GenericSpriteFont Gsf { get; private set; }
        public SpriteFont FontSmall { get; private set; }
        public GenericSpriteFont GsfSmall { get; private set; }

        //Inputs
        public Point MousePos { get; private set; } = new();
        public MouseState MouseState { get; private set; }
        public MouseState MouseStateOld { get; private set; }
        public KeyboardState KeyboardState { get; private set; }
        public KeyboardState KeyboardStateOld { get; private set; }

        public Menu Menu { get => menu; set {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            var oldMenu = menu;
            menu?.Destroy();
            menu = value;
            menu.LoadContent();
            
        } }

        public Game1() {
            GraphicsDeviceManager = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            ih = new InputHandler(this);
            Window.AllowUserResizing = true;
        }

        protected override void Initialize() {
            // TODO: Add your initialization logic here
            base.Initialize();
        }
        protected override void LoadContent() {
            SpriteBatch = new SpriteBatch(base.GraphicsDevice);
            //Font
            var fontBakeResult = TtfFontBaker.Bake(File.ReadAllBytes(@"C:\\Windows\\Fonts\arial.ttf"),
                25, 1024, 1024, [
                    CharacterRange.BasicLatin,
                    CharacterRange.Latin1Supplement,
                    CharacterRange.LatinExtendedA,
                    CharacterRange.Cyrillic
                ]
            );
            Font = fontBakeResult.CreateSpriteFont(base.GraphicsDevice);
            Gsf = new GenericSpriteFont(Font);
            var fontBakeResultSmall = TtfFontBaker.Bake(File.ReadAllBytes(@"C:\\Windows\\Fonts\arial.ttf"),
                12, 512, 512, [
                    CharacterRange.BasicLatin,
                    CharacterRange.Latin1Supplement,
                    CharacterRange.LatinExtendedA,
                    CharacterRange.Cyrillic
                ]
            );
            FontSmall = fontBakeResultSmall.CreateSpriteFont(base.GraphicsDevice);
            GsfSmall = new GenericSpriteFont(FontSmall);


            defaultUiStyle = new UntexturedStyle(SpriteBatch);
            defaultUiStyle.Font = GsfSmall;
            defaultUiStyle.PanelColor = Color.DarkGray;
            Menu = new InGameMenu(this);
        }
        
        protected override void Update(GameTime gameTime) {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            MouseStateOld = MouseState;
            MouseState = Mouse.GetState();
            KeyboardStateOld = KeyboardState;
            KeyboardState = Keyboard.GetState();

            ih.Update();
            Menu?.Update(gameTime);
            
            //Refresh the mouse state for the next frame
            base.Update(gameTime);
        }

        
        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Menu?.Draw(gameTime);
            Menu?.Draw2D(gameTime);
            base.Draw(gameTime);
        }
    }
}