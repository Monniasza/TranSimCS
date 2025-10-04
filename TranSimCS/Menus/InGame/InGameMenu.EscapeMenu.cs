using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui.Elements;
using MLEM.Ui.Style;
using NLog;
using TranSimCS.Worlds;

namespace TranSimCS.Menus.InGame {

    //Escape menu part of the InGameMenu
    public partial class InGameMenu {
        public class EscapeMenu: Panel {
            public readonly InGameMenu parent;
            private UiStyle buttonStyle;
            private static Logger Logger = LogManager.GetCurrentClassLogger();

            internal EscapeMenu(InGameMenu parent) : base(MLEM.Ui.Anchor.Center, new(0.5f, 0.5f), false, true, true) {
                this.parent = parent;
                buttonStyle = new UiStyle(parent.UiSystem.Style);
                buttonStyle.Font = parent.Game.Gsf;
                NewOption("Back to game", () => parent.Overlay = null!);
                NewOption("Reset", ResetWorld);
                NewOption("Load", LoadSave);
                NewOption("Save", SaveGame);
                NewOption("Exit to desktop (!)", Exit);
            }

            public void LoadSave() {
                var saveDialog = new SaveGameDialog(parent, Program.SaveRoot, false);
                var dialog = new ConfirmLoseDialog(
                    () => Program.Await(SaveToFile(saveDialog)),
                    LoadSaveDialog,
                    () => parent.Overlay = this, parent.Game
                );
                parent.Overlay = dialog;
            }

            public void LoadSaveDialog() {
                var loadDialog = new SaveGameDialog(parent, Program.SaveRoot, true);
                parent.Overlay = loadDialog;
                LoadSaveFromFile(loadDialog);
            }

            public async Task<bool> LoadSaveFromFile(SaveGameDialog dialog) {
                parent.Overlay = dialog;
                TaskCompletionSource<string?> tcs = new();
                dialog.OnSave += (file) => tcs.TrySetResult(file);
                string filename = await tcs.Task;
                if (filename == null) {
                    parent.Overlay = this;
                    return false;
                }
                try {
                    InGameMenu newMenu = new InGameMenu(parent.Game);
                    newMenu.LoadWorldFromFile(filename);
                    //newMenu.LoadContent();
                    parent.Game.Menu = newMenu;
                } catch (Exception e) {
                    OptionsDialog.FromError(parent, e, this).Show();
                    Logger.Error(e);
                    return false;
                }
                return true;
            }
            public async Task<bool> SaveToFile(SaveGameDialog dialog) {
                TaskCompletionSource<string?> tcs = new();
                dialog.OnSave += (file) => tcs.TrySetResult(file);
                string filename = await tcs.Task;
                if (filename == null) {
                    parent.Overlay = this;
                    return false;
                }
                try {
                    parent.World.SaveToFile(filename);
                } catch (Exception e) {
                    OptionsDialog.FromError(parent, e, this).Show();
                    return false;
                }
                parent.Overlay = this;
                return true;
            }

            public void CreateSaveConfirm(Action afterSaving) {
                var saveDialog = new SaveGameDialog(parent, Program.SaveRoot, false);
                var dialog = new ConfirmLoseDialog(
                    () => Program.Await(LoadSaveFromFile(saveDialog)),
                    () => parent.Overlay = null,
                    () => parent.Overlay = this, parent.Game
                );
            }

            public void SaveGame() {
                var saveDialog = new SaveGameDialog(parent, Program.SaveRoot, false);
                saveDialog.OnSave += (file) => {
                    if (file == null) parent.Overlay = this;
                    else parent.World.SaveToFile(file);
                };
                parent.Overlay = saveDialog;
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
