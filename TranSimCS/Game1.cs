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
using MLEM.Misc;
using MLEM.Textures;
using MLEM.Ui.Elements;

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


            defaultUiStyle = CreateUiStyle();

            

            MlemPlatform.Current = MlemPlatform.Current = new MlemPlatform.DesktopGl<TextInputEventArgs>((w, c) => w.TextInput += c);
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

        private UntexturedStyle CreateUiStyle() {
            var s = new UntexturedStyle(SpriteBatch);
            s.Font = GsfSmall;
            s.PanelColor = Color.DarkGray;
            var panelTex = Content.Load<Texture2D>("ui/panel");
            var panel9patch = new NinePatch(panelTex, 4);
            var panelTex2 = Content.Load<Texture2D>("ui/panelsmokeblack");
            var panel9patch2 = new NinePatch(panelTex2, 4);
            var outline = new NinePatch(Content.Load<Texture2D>("ui/outline"), 6);

            //s.ButtonTexture = panel9patch;
            //Workaround for no ButtonColor - going to go in future
            s.ButtonTexture = panel9patch2;

            s.ButtonHoveredTexture = panel9patch;
            s.ButtonHoveredColor = Colors.SemiClearAzure;
            s.ButtonDisabledColor = Colors.SemiClearGray;
            s.ButtonDisabledTexture = panel9patch;

            s.PanelColor = Colors.SmokedGlass;
            s.PanelTexture = panel9patch;

            s.TextFieldTexture = panel9patch2;
            s.TextFieldHoveredTexture = panel9patch;
            s.TextFieldHoveredColor = Colors.SemiClearAzure;
            s.TextFieldCaretWidth = 2;

            s.CheckboxTexture = panel9patch2;
            s.CheckboxHoveredColor = Colors.SemiClearAzure;
            s.CheckboxDisabledColor = Colors.SemiClearGray;
            s.CheckboxCheckmark = new TextureRegion(Content.Load<Texture2D>("ui/check"));

            s.SelectionIndicator = outline;

            return s;
        }
    }
}