using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Ui.Elements;
using MLEM.Ui.Style;
using TranSimCS.Save;
using TranSimCS.Worlds;

namespace TranSimCS.Menus.InGame {

    //Escape menu part of the InGameMenu
    public partial class InGameMenu {
        public class EscapeMenu : MLEM.Ui.Elements.Panel {
            public readonly InGameMenu parent;
            private UiStyle buttonStyle;

            internal EscapeMenu(InGameMenu parent) : base(MLEM.Ui.Anchor.Center, new(0.5f, 0.5f), false, true, true) {
                this.parent = parent;
                buttonStyle = new UiStyle(parent.UiSystem.Style);
                buttonStyle.Font = parent.Game.Gsf;
                NewOption("Back to game", () => parent.Overlay = null!);
                NewOption("Reset", ResetWorldButton);
                NewOption("Load", LoadSaveButton);
                NewOption("Save", SaveGameButton);
                NewOption("Exit to desktop", ExitButton);
            }

            private void GoBack() => parent.Overlay = this;

            public void LoadSaveButton() => ShowConfirmDialog(SaveGame, LoadSave, GoBack);
            public void LoadSave() {

            }

            public void SaveGameButton() => SaveGame();
            public bool SaveGame() {
                Directory.CreateDirectory(Program.SaveDirectory);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = Path.Combine(Program.SaveDirectory, $"save_{timestamp}.json");
                var world = parent.World;
                var serializer = world.CreateSerializer();
                Program.SerializeToFile(filename, world, serializer);

                return true;
            }

            public void ResetWorldButton() {
                WorldGenerator.SetUpExampleWorld(parent.World);
                parent.Overlay = null;
            }

            public void ExitButton() {

                 parent.Game.Exit();
            }

            private void ShowConfirmDialog(Func<bool> save, Action proceed, Action cancel) {
                parent.Overlay = new ConfirmLoseDialog(
                    save,
                    () => {
                        parent.Overlay = null;
                        proceed();
                    },
                    () => {
                        parent.Overlay = this;
                        cancel();
                    },
                    parent.Game
                );
            }

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
