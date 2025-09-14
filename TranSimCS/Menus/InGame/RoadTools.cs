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

namespace TranSimCS.Menus.InGame {
    public class RoadTools : Panel {
        public InGameMenu Game { get; private set; }
        public Checkbox flattenTilt { get; private set; }
        public Checkbox flattenIncline { get; private set; }

        public StyleProp<TextureRegion> LoadStyleProp(string name) {
            return new StyleProp<TextureRegion>(new TextureRegion(Game.Game.Content.Load<Texture2D>(name)));

        }

        public RoadTools(InGameMenu game, Anchor anchor, Vector2 size)
            : base(anchor, size, true) {
            Game = game;
            Checkbox anarchyCheck = new Checkbox(Anchor.AutoInline, new(21, 21), "", false);
            anarchyCheck.Checkmark = LoadStyleProp("ui/anarchy2");
            anarchyCheck.UncheckColor = Color.LightGray;
            anarchyCheck.CheckColor = Color.Orange;
            AddChild(anarchyCheck);

            flattenTilt = new Checkbox(Anchor.AutoInline, new(21, 21), "", false);
            flattenTilt.AddTooltip("Flatten tilt");
            flattenTilt.Checkmark = LoadStyleProp("ui/flatTilt");
            flattenTilt.UncheckColor = Color.Gray;
            AddChild(flattenTilt);

            flattenIncline = new Checkbox(Anchor.AutoInline, new(21, 21), "", false);
            flattenIncline.AddTooltip("Flatten inclination");
            flattenIncline.Checkmark = LoadStyleProp("ui/flatIncline");
            flattenIncline.UncheckColor = Color.Gray;
            AddChild(flattenIncline);

            //Modes
            CreateModeButton(new StraightMode(), "ui/line");
            CreateModeButton(new CircMode(), "ui/curved");
            CreateModeButton(new SBendMode(), "ui/sbend");
            //CreateModeButton("ui/sbend3C", "S-bend, custom direction");
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
