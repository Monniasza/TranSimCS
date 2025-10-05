using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Ui;
using MLEM.Ui.Elements;
using TranSimCS.Worlds;

namespace TranSimCS.Menus.InGame {
    public partial class InGameMenu {
        public override void OnRequestClose() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// A dialog with three buttons: Save, Don't Save, Cancel.
        /// <list type="bullet">
        ///     <item>Clicking "Don't Save" proceeds to the <paramref name="ok"/>, without saving the game.</item>
        ///     <item>Clicking "Save" opens a dialog to save the game file.
        ///     <br/>If the dialog is accepted, then the dialog saves the game with the <paramref name="save"/> and then proceeds to <paramref name="ok"/>.</item>
        ///     <br/>If the dialog is not accepted, then the user returns to the dialog.
        ///     <item>Clicking "Cancel" closes the dialog with the <paramref name="cancel"/>.</item>
        /// </list>
        /// </summary>
        public class ConfirmLoseDialog: Panel {
            private Func<bool> save;
            private Action ok;
            private Action cancel;
            private Game1 game;
            private static readonly Vector2 buttonSize = new(Maths._1_3, 50);

            public ConfirmLoseDialog(Func<bool> save, Action ok, Action cancel, Game1 game)
                : base(MLEM.Ui.Anchor.Center, new(0.5f, 200)){
                this.game = game;
                this.save = save;
                this.ok = ok;
                this.cancel = cancel;

                Paragraph paragraph = new Paragraph(MLEM.Ui.Anchor.TopCenter, 1, "Unsaved changes. Do you want to proceed?", true);
                paragraph.RegularFont = game.Gsf;
                AddChild(paragraph);

                CreateButton(Anchor.BottomLeft, "Save", SaveClicked);
                var dontSave = CreateButton(Anchor.BottomCenter, "Don't Save", DiscardClicked);
                dontSave.NormalColor = Color.Red;
                CreateButton(Anchor.BottomRight, "Cancel", CancelClicked);
            }

            private Button CreateButton(Anchor anchor, String name, GenericCallback callback) {
                Button button = new Button(anchor, buttonSize, name);
                button.OnPressed += callback;
                AddChild(button);
                return button;
            }

            private void SaveClicked(Element e) {
                var taskFactory = TaskFactoryFactory<object?>.GetFactory();
                taskFactory.StartNew((x) => {
                    var result = save();
                    if (result) ok();
                    else cancel();
                    return null;
                }, null);                
            }
            private void CancelClicked(Element e) {
                cancel();
            }
            private void DiscardClicked(Element e) {
                ok();
            }
        }
    }
}
