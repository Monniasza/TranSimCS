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
            SpriteFont TryBakeOrFallback(string relativeTtfPath, int size, int atlasW, int atlasH, CharacterRange[] ranges) {
                // Local helper to try baking from a single path
                SpriteFont TryBakeFromPath(string path) {
                    var bytes = File.ReadAllBytes(path);
                    var baked = TtfFontBaker.Bake(bytes, size, atlasW, atlasH, ranges);
                    return baked.CreateSpriteFont(base.GraphicsDevice);
                }
                try {
                    var ttfPath = Path.Combine(Content.RootDirectory, relativeTtfPath);
                    if (File.Exists(ttfPath))
                        return TryBakeFromPath(ttfPath);
                } catch {
                    // ignore and fall back to system fonts below
                }
                // Fallback to common system font locations per OS
                var candidates = new List<string>();
                if (OperatingSystem.IsWindows()) {
                    var fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
                    candidates.AddRange(new[] {
                        Path.Combine(fonts, "arial.ttf"),
                        Path.Combine(fonts, "segoeui.ttf"),
                        Path.Combine(fonts, "tahoma.ttf"),
                        Path.Combine(fonts, "calibri.ttf"),
                    });
                } else if (OperatingSystem.IsLinux()) {
                    candidates.AddRange(new[] {
                        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                        "/usr/share/fonts/truetype/freefont/FreeSans.ttf",
                        "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf",
                        "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
                    });
                } else if (OperatingSystem.IsMacOS()) {
                    candidates.AddRange(new[] {
                        "/Library/Fonts/Arial.ttf",
                        "/System/Library/Fonts/Supplemental/Arial.ttf",
                        "/Library/Fonts/Helvetica.ttc"
                    });
                }
                foreach (var path in candidates) {
                    try {
                        if (File.Exists(path))
                            return TryBakeFromPath(path);
                    } catch {
                        // try next candidate
                    }
                }
                throw new FileNotFoundException("No usable TTF font found. Add a TTF to Content/Fonts (e.g., Roboto-Regular.ttf) or install a common system font (Arial/DejaVuSans).");
            }
            var ranges = new[] {
                CharacterRange.BasicLatin,
                CharacterRange.Latin1Supplement,
                CharacterRange.LatinExtendedA,
                CharacterRange.Cyrillic
            };
            // Attempt to load a cross-platform TTF shipped with the game
            // Place a TTF at Content/Fonts/Roboto-Regular.ttf for portability
            Font = TryBakeOrFallback(Path.Combine("Fonts", "Roboto-Regular.ttf"), 25, 1024, 1024, ranges);
            Gsf = new GenericSpriteFont(Font);
            FontSmall = TryBakeOrFallback(Path.Combine("Fonts", "Roboto-Regular.ttf"), 12, 512, 512, ranges);
            GsfSmall = new GenericSpriteFont(FontSmall);

            defaultUiStyle = CreateUiStyle();   

            MlemPlatform.Current = MlemPlatform.Current = new MlemPlatform.DesktopGl<TextInputEventArgs>((w, c) => w.TextInput += c);
            Menu = new InGameMenu(this);

            KeyPromptMapper.SetUpKeyPrompts(Content);
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

            s.RadioColor = Colors.SmokedGlass;
            s.RadioTexture = panel9patch;
            s.RadioHoveredColor = Colors.SemiClearAzure;
            s.RadioHoveredTexture = panel9patch;
            s.RadioUncheckedColor = Color.Gray;
            s.RadioCheckColor = Color.White;

            s.SelectionIndicator = outline;

            return s;
        }
    }
}