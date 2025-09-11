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
        public static StyleProp<TextureRegion> LoadStyleProp(InGameMenu game, string name) {
            return new StyleProp<TextureRegion>(new TextureRegion(game.Game.Content.Load<Texture2D>(name)));

        }

        public RoadTools(InGameMenu game, Anchor anchor, Vector2 size)
            : base(anchor, size, true) {

            Checkbox anarchyCheck = new Checkbox(Anchor.AutoInline, new(1, 21), "Anarchy", false);
            anarchyCheck.Checkmark = LoadStyleProp(game, "ui/anarchy2");
            AddChild(anarchyCheck);
        }
    }
}
