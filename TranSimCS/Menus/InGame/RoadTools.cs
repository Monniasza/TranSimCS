using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Textures;
using MLEM.Ui;
using MLEM.Ui.Elements;
using MLEM.Ui.Style;
using TranSimCS.Worlds;

namespace TranSimCS.Menus.InGame {
    public class RoadTools : Panel {
        public InGameMenu Game { get; private set; }
        public Checkbox flattenTilt { get; private set; }
        public Checkbox flattenIncline { get; private set; }
        public Checkbox anarchyCheck { get; private set; }
        public TextField heightStepField { get; private set; }
        public TextField HeightField { get; private set; }
        public Property<float> HeightStep { get; private set; }
        public Property<float> Height {  get; private set; }

        public StyleProp<TextureRegion> LoadStyleProp(string name) {
            return new StyleProp<TextureRegion>(new TextureRegion(Game.Game.Content.Load<Texture2D>(name)));
        }

        public RoadTools(InGameMenu game, Anchor anchor, Vector2 size)
            : base(anchor, size, true) {
            Game = game;

            var settingsLabel = new Paragraph(Anchor.AutoInline, 0.5f, "Settings");
            AddChild(settingsLabel);
            anarchyCheck = CreateCheck("Anarchy", "ui/anarchy2", Color.Orange);
            flattenTilt = CreateCheck("Flatten tilt", "ui/flatTilt");
            flattenIncline = CreateCheck("Flatten inclination", "ui/flatIncline");

            //Modes
            var modesLabel = new Paragraph(Anchor.AutoInlineBottom, 0.5f, "Modes");
            AddChild(modesLabel);
            CreateModeButton(new StraightMode(), "ui/line");
            CreateModeButton(new CircMode(), "ui/curved");
            CreateModeButton(new SBendMode(), "ui/sbend");
            //CreateModeButton("ui/sbend3C", "S-bend, custom direction");

            //Height adjustment
            HeightStep = new Property<float>(10, "heightStep");
            heightStepField = UI.SetUpFloatProp("Height step [m]", this, HeightStep);
            Height = new Property<float>(0.1f, "height");
            HeightField = UI.SetUpFloatProp("Height [m]", this, Height);
        }

        public Checkbox CreateCheck(String name, String icon, Color? checkColor = null, Color? uncheckColor = null) {
            return UI.CreateCheck(Game, this, name, icon, checkColor, uncheckColor);
        }

        public RadioButton CreateModeButton(RoadMode mode, String icon) {
            RadioButton radio = new RadioButton(Anchor.AutoInline, new(21, 21), "", false, "mode");
            radio.Checkmark = LoadStyleProp(icon);
            radio.UncheckColor = Color.Gray;
            radio.CheckColor = Color.White;
            radio.AddTooltip((p) => mode.Name);
            radio.OnSelected += (a) => Game.RoadCreationTool.Mode = mode;
            AddChild(radio);
            return radio;
        }
    }
}
