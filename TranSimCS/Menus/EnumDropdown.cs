using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Ui;
using MLEM.Ui.Elements;
using TranSimCS.Worlds;

namespace TranSimCS.Menus {
    public class EnumDropdown<T> : Dropdown where T : Enum {
        public readonly Property<T> SelectedValueProp;
        public T SelectedValue { get => SelectedValueProp.Value; set => SelectedValueProp.Value = value; }

        public EnumDropdown(Anchor anchor, Vector2 size, T defaultValue, string? tooltipText = null, float panelHeight = 0, bool scrollPanel = false, bool autoHidePanelScrollbar = true)
            : base(anchor, size, "", tooltipText, panelHeight, scrollPanel, autoHidePanelScrollbar) {

            SelectedValueProp = new Property<T>(defaultValue, "value");

            var values = Enum.GetValues(typeof(T));
            var names = Enum.GetNames(typeof(T));

            var displayName = GetName(defaultValue);
            Text.Text = displayName;

            //Create options for the enum
            for (int i = 0; i < values.Length; i++) {
                var value = (T)values.GetValue(i);
                var name = names[i];

                Button button = new Button(Anchor.AutoLeft, new(1, 20), name);
                button.OnPressed += (e) => SelectedValue = value;
                AddElement(button);
            }

            SelectedValueProp.ValueChanged += (s, e) => {
                var value = e.NewValue;
                var displayName = GetName(value);
                Text.Text = displayName;
            };   
            
        }

        public string GetName(T value) => Enum.GetName(typeof(T), value);
    }
}
