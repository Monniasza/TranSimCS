using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Input;
using MLEM.Ui.Elements;

namespace TranSimCS.Menus.InGame {
    public class KeyBindPanel: Panel {
        private readonly InGameMenu Menu;
        private (object[], string)[] lastKeys = [];
        
        public KeyBindPanel(InGameMenu menu): base(MLEM.Ui.Anchor.TopRight, new(0.5f, 40), true) {
            Menu = menu;
        }

        public void Update() {
            var tool = Menu.configuration.Tool;
            var toolKeys = (tool?.PromptKeys()) ?? [];
            var menuKeys = Menu.FixedKeys();
            var keybinds = toolKeys.Concat(menuKeys).ToArray();
            var keybindsChanged = !Equality.DeepArrayEqualsWithNull(lastKeys, keybinds);

            if (keybindsChanged) {
                //Keybinds changed
                lastKeys = keybinds;
                RemoveChildren();
                foreach (var keybind in keybinds ?? []) {

                    var keys = keybind.Item1;
                    bool firstInLine = true;
                    foreach (var key in keys) {
                        var tex = KeyPromptMapper.GetPrompt(key);
                        if (tex != null) {
                            Image img = new Image(firstInLine ? MLEM.Ui.Anchor.AutoLeft : MLEM.Ui.Anchor.AutoInline, new(1, 1), tex, true);
                            AddChild(img);
                            firstInLine = false;
                        }
                    }
                    var desc = keybind.Item2;
                    var paragraph = new Paragraph(MLEM.Ui.Anchor.AutoInline, 0.5f, desc ?? "", true);
                    AddChild(paragraph);
                }
                SetAreaDirty();
            }
            
        }
    }
}
