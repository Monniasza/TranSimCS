using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Ui;
using MLEM.Ui.Elements;
using TranSimCS.Model;

namespace TranSimCS.Menus.InGame {
    public partial class InGameMenu {
        public class SaveGameDialog: Panel{
            public event Action<string?>? OnSave;
            private TextField directoryTextEntry;
            private Panel contentsPanel;
            private TextField fileNameEntry;
            private bool isLoad;
            private InGameMenu menu;
            
            public SaveGameDialog(InGameMenu menu, string path, bool isLoad, string? buttonLabel = null, string? saveName = null)
                : base(Anchor.Center, new(0.5f, 0.5f)) {

                this.isLoad = isLoad;
                this.menu = menu;

                buttonLabel ??= (isLoad ? "Open" : "Save");

                //Directory field
                directoryTextEntry = new TextField(Anchor.AutoLeft, new(0.8f, 20));
                directoryTextEntry.OnEnterPressed += ChangePath;
                directoryTextEntry.SetText(path);
                AddChild(directoryTextEntry);

                var toParent = new Button(Anchor.AutoInline, new(0.2f, 20), "To parent directory");
                toParent.OnPressed += ToParent;
                AddChild(toParent);

                contentsPanel = new Panel(Anchor.AutoInline, new(1, 0.8f), false, true);
                AddChild(contentsPanel);

                fileNameEntry = new TextField(Anchor.AutoLeft, new(0.8f, 20));
                fileNameEntry.OnEnterPressed += OkPressed;
                fileNameEntry.SetText(saveName ?? "");
                AddChild(fileNameEntry);

                var okButton = new Button(Anchor.AutoInline, new(0.1f, 20), "OK");
                okButton.OnPressed += OkPressed;
                okButton.NormalColor = Color.Green;
                AddChild(okButton);

                var cancelButton = new Button(Anchor.AutoInline, new(0.1f, 20), "Cancel");
                cancelButton.OnPressed += (x) => Handle(null);
                cancelButton.NormalColor = Color.Red;
                AddChild(cancelButton);

                ChangePath(okButton);
            }

            private void OkPressed(Element e) => Handle(Path.Combine(directoryTextEntry.Text, fileNameEntry.Text));

            private void Invoke(string? path) => OnSave?.Invoke(path);

            private void Handle(string? path) {
                if (path == null) {
                    OnSave?.Invoke(null);
                    return;
                }

                var fileExists = File.Exists(path);
                if (isLoad) {
                    if (!fileExists) {
                        //Warn the user that file does not exist
                        new OptionsDialog(menu, this, $"The file ${path} does not exist.",
                            ("OK", Program.DoNothing)
                        ).Show();
                    } else Invoke(path);
                } else {
                    if (fileExists) {
                        //Warn about overwriting a file
                        new OptionsDialog(menu, null, $"You are about to overwrite ${path}. Do you want to proceed?",
                            ("Yes", () => Invoke(path)),
                            ("No", () => menu.Overlay = this)
                        ).Show();
                    } else Invoke(path);
                }
            }

            private void ToParent(Element element) {
                var dir = directoryTextEntry.Text;
                string parent = new DirectoryInfo(dir).Parent?.FullName ?? dir;
                directoryTextEntry.SetText(parent);
                ChangePath(element);
            }

            private void ChangePath(Element element) {
                contentsPanel.RemoveChildren();
                try {
                    var dirInfo = new DirectoryInfo(directoryTextEntry.Text);
                    foreach(var dir in dirInfo.GetDirectories()) CreateDirButton(dir);
                    foreach(var dir in dirInfo.GetFiles()) CreateFileButton(dir);
                } catch(Exception e) {
                    contentsPanel.RemoveChildren();
                    string message = e.ToString() + "\n Refresh or go to a different directory";
                    Paragraph errorParagraph = new Paragraph(Anchor.AutoLeft, 1, message);
                    errorParagraph.TextColor = Color.Red;
                    contentsPanel.AddChild(errorParagraph);
                }
            }

            private void CreateDirButton(DirectoryInfo dir) {
                var button = new Button(Anchor.AutoLeft, new(1, 20), dir.Name);
                button.OnPressed = (e) => {
                    directoryTextEntry.SetText(dir.FullName);
                    ChangePath(e);
                };
                contentsPanel.AddChild(button);
            }
            private void CreateFileButton(FileInfo file) {
                var button = new Button(Anchor.AutoLeft, new(1, 20), file.Name);
                button.OnPressed = (e) => {
                    fileNameEntry.SetText(file.FullName);
                };
                contentsPanel.AddChild(button);
            }

            public async Task<string> WaitAndHandle(string filepath) {
                var tcs = new TaskCompletionSource<string>();

                Action<string> handler = (value) => tcs.TrySetResult(value);
                OnSave += handler;
                var result = await tcs.Task;
                OnSave -= handler;
                return result;
            }
        }
    }
}
