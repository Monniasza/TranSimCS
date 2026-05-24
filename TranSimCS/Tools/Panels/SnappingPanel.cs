using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Ui.Elements;
using MLEM.Ui;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;

namespace TranSimCS.Tools.Panels {
    public class SnappingPanel: Panel {
        public InGameMenu Game { get; private set; }
        public Checkbox isYLocal { get; private set; }
        public Checkbox isInfinite { get; private set; }
        public NumberField cellSize {  get; private set; }
        public TextField cellCount { get; private set; }


        public SnappingPanel(InGameMenu game)
           : base(Anchor.AutoLeft, new(1, 1), true) {
            this.Game = game;

            var settingsLabel = new Paragraph(Anchor.AutoInline, 0.4f, "Settings");
            AddChild(settingsLabel);
            isYLocal = CreateCheck("Global Y reference", "ui/ylocal");
            isYLocal.AddProperty(game.configuration.SnapGrid.IsYLocalProp);
            isInfinite = CreateCheck("Infinite snapping grid", "ui/node");
            isInfinite.AddProperty(game.configuration.SnapGrid.IsInfiniteProp);

            cellSize = UI.SetUpFloatProp("Cell size", this, game.configuration.SnapGrid.CellSizeProp);
            cellCount = UI.SetUpUIntProp("Snapping cell count", this, game.configuration.SnapGrid.CellCountProp);
        }

        public Checkbox CreateCheck(string name, string icon, Color? checkColor = null, Color? uncheckColor = null) {
            return UI.CreateCheck(Game, this, name, icon, checkColor, uncheckColor);
        }
    }
}
