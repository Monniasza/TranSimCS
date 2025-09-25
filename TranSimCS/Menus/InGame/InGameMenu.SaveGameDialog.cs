using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui;
using MLEM.Ui.Elements;

namespace TranSimCS.Menus.InGame {
    public partial class InGameMenu {
        public class SaveGameDialog: Panel{
            private event Action<string>? OnSave;
            private TextField directoryTextEntry;
            private Panel contentsPanel;
            
            public SaveGameDialog(string path, string buttonLabel, string? saveName = null)
                : base(Anchor.Center, new(0.5f, 0.5f)) {

                //Directory field
                directoryTextEntry = new TextField(Anchor.AutoLeft, new(0.8f, 20));
                directoryTextEntry.OnEnterPressed += ChangePath;
                AddChild(directoryTextEntry);

                var toParent = new Button(Anchor.AutoInline, new(0.2f, 20), "To parent directory");
                toParent.OnPressed += ToParent;
                AddChild(toParent);

                contentsPanel = new Panel(Anchor.AutoInline, new(1, 0.8f), false, true);
                AddChild(contentsPanel);
            }

            private void ToParent(Element element) {
                var dir = directoryTextEntry.Text;
                string parent = new DirectoryInfo(dir).Parent.FullName;
                directoryTextEntry.SetText(parent);
                ChangePath(element);
            }

            private void ChangePath(Element element) {
                contentsPanel.Children.Clear();
                try {
                    var dirInfo = new DirectoryInfo(directoryTextEntry.Text);
                    foreach(var dir in dirInfo.GetDirectories()) CreateDirButton(dir);
                    foreach(var dir in dirInfo.GetFiles()) CreateFileButton(dir);
                } catch {

                }
            }

            private void CreateDirButton(DirectoryInfo dir) {
                var button = new Button(Anchor.AutoLeft, new(1, 20), dir.Name);
                button.OnPressed = (e) => {
                    directoryTextEntry.SetText(dir.FullName);
                    ChangePath(e);
                };
            }
            private void CreateFileButton(FileInfo file) {

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
