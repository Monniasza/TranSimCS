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

            //Modes
            CreateModeButton("ui/line", "Straight");
            CreateModeButton("ui/curved", "Circular arc");
            CreateModeButton("ui/sbend", "S-bend, same-direction");
            CreateModeButton("ui/sbend3C", "S-bend, custom direction");
        }

        public RadioButton CreateModeButton(/*RoadMode mode,*/ String icon, String name) {
            RadioButton radio = new RadioButton(Anchor.AutoInline, new(21, 21), "", false, "mode");
            radio.Checkmark = LoadStyleProp(icon);
            radio.UncheckColor = Color.Gray;
            radio.CheckColor = Color.White;
            radio.AddTooltip(name);
            AddChild(radio);
            return radio;
        }
    }
}
