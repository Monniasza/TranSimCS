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
            
            public SaveGameDialog(string path, string buttonLabel, string? saveName = null)
                : base(Anchor.Center, new(0.5f, 0.5f)) {
                
                //Text field for the directory
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
