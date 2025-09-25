using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui;
using MLEM.Ui.Elements;

namespace TranSimCS.Menus.InGame {
    public class OptionsDialog: Panel {
        public event Action<int>? OptionSelected;
        private InGameMenu menu;

        public OptionsDialog(InGameMenu menu, Element? returnTo, string message, params string[] options)
        : base(Anchor.Center, new(0.5f, 0.5f), true) {
            if(options.Length == 0) throw new ArgumentException("There must be at least one option", nameof(options));
            this.menu = menu;
            var game = menu.Game;
            var gsf = game.Gsf;

            var paragraph = new Paragraph(Anchor.AutoLeft, 1, message);
            AddChild(paragraph);

            for (int i = 0; i < options.Length; i++) {
                var option = options[i];
                var j = i;
                var button = new Button((i == 0) ? Anchor.AutoLeft : Anchor.AutoInline, new(1f / options.Length), option);
                GenericCallback callback = (e) => OptionSelected?.Invoke(j);
                button.OnPressed += callback;
                AddChild(button);
            }
            OptionSelected += (i) => menu.Overlay = returnTo ?? menu.Overlay;
        }

        public OptionsDialog(InGameMenu menu, Element returnTo, string message, params (string, Action)[] options)
            : this(menu, returnTo, message, options.Select(option => option.Item1).ToArray()) {
            OptionSelected += index => options[index].Item2();
        }

        public static OptionsDialog FromError(InGameMenu menu, Exception error, Element? returnTo = null) {
            return new OptionsDialog(menu, returnTo, error.ToString(), "OK");
        }

        public void Show() => menu.Overlay = this;
    }
}
