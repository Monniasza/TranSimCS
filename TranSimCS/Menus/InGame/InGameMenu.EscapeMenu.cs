using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui.Elements;
using MLEM.Ui.Style;
using TranSimCS.Worlds;

namespace TranSimCS.Menus.InGame {

    //Escape menu part of the InGameMenu
    public partial class InGameMenu {
        public class EscapeMenu: Panel {
            public readonly InGameMenu parent;
            private UiStyle buttonStyle;

            internal EscapeMenu(InGameMenu parent) : base(MLEM.Ui.Anchor.Center, new(0.5f, 0.5f), false, true, true) {
                this.parent = parent;
                buttonStyle = new UiStyle(parent.UiSystem.Style);
                buttonStyle.Font = parent.Game.Gsf;
                NewOption("Back to game", () => parent.Overlay = null);
                NewOption("Reset", ResetWorld);
                NewOption("Load", LoadSave);
                NewOption("Save", SaveGame);
                NewOption("Exit to desktop (!)", Exit);
            }

            public void LoadSave() {

            }

            public void SaveGame() {

            }

            public void ResetWorld() {
                WorldGenerator.SetUpExampleWorld(parent.World);
                parent.Overlay = null;
            }

            public void Exit() => parent.Game.Exit();

            private Button NewOption(String text, Action? action) {
                Button result = new Button(MLEM.Ui.Anchor.AutoLeft, new(1, 40), text);
                result.OnPressed += (s) => action?.Invoke();
                result.Text.RegularFont = parent.Game.Gsf;
                AddChild(result);
                return result;
            }
        }
    }
}
