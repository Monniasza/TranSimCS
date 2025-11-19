using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Graphics;
using MLEM.Ui;
using MLEM.Ui.Elements;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using TranSimCS.Menus.InGame;

namespace TranSimCS.Menus.MainMenu {
    public class MainMenu : Menu {
        public MainMenu(Game1 menu) : base(menu) {

        }
        public override void Destroy() {
            //unused
        }

        public override void Draw(GameTime time) {
            //unused
        }

        public override void Draw2D(GameTime time) {

            //Find the desired position of the banner. Skewed half the height
            var dimensions = Game.Window.ClientBounds.Size;

            var desiredH = dimensions.Y * 0.25f;
            var desiredW = dimensions.X * 0.9f;

            //Ratio : 9 * 21.25; / 36/125;

            var sharesH = desiredH / 36;
            var sharesW = desiredW / 125;
            var shares = MathF.Min(sharesW, sharesH);

            var barwidth = shares * 116;
            var height = shares * 36;
            var width = shares * 125;
            var skew = shares * 18;
            var hskew = shares * 9;

            var centerX = dimensions.X * 0.5f;
            var centerY = dimensions.Y * 0.25f;

            var ulcornerX = centerX - barwidth + (skew * 0.75f);
            var ulcornerY = centerY - skew;

            Matrix3x2 matrix = Matrix3x2.Identity;
            matrix.Z = new(ulcornerX, ulcornerY);
            matrix.Y = new(-skew, height);
            matrix.X = new(barwidth, 0);
            matrix.X /= 16;
            matrix.Y /= 16;

            var bannerTexture = Game.Content.Load<Texture2D>("banner");
            SpriteBatchContext sbc = new() {
                TransformMatrix = matrix.ToMatrix(),
                SamplerState = SamplerState.PointClamp
            };

            Game.SpriteBatch.Begin(sbc);
            Game.SpriteBatch.Draw(bannerTexture, Vector2.Zero, Color.White);
            Game.SpriteBatch.End();

            UiSystem.Draw(time, Game.SpriteBatch);
        }

        public Panel MainPanel { get; private set; }
        public override void LoadContentOverride() {
            Panel panel = new(MLEM.Ui.Anchor.Center, new(0.5f, 0.5f), true);
            MainPanel = panel;
            UiSystem.Add("main", panel);

            NewMenuOption("mainmenu/start", "Start the game", () => {
                Game.Menu = new InGameMenu(Game);
            });
            NewMenuOption("mainmenu/load", "Load a save", () => {
                //Nothing yet
            });
            NewMenuOption("mainmenu/settings", "Settings", () => {
                //Nothing yet
            });
            NewMenuOption("mainmenu/desktop", "Exit to desktop", () => {
                Game.Exit();
            });
        }

        public void NewMenuOption(string tex, string title, Action action) {
            var imageAnchor = Anchor.AutoInline;
            var imageScale = new Vector2(32, 32);
            PictureButton startButton = new PictureButton(MLEM.Ui.Anchor.AutoLeft, new(1, 40), UI.CreateTextureCallback(tex), imageAnchor, imageScale, title);
            startButton.PictureBeforeText();
            startButton.Text.RegularFont = Game.Gsf;
            startButton.OnPressed += e => action();
            MainPanel.AddChild(startButton);
        }

        public override void OnRequestClose() {
            //unused
        }

        public override void Update(GameTime time) {
            UiSystem.Update(time);
        }
    }
}
