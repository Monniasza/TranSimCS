using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Textures;
using MLEM.Ui;
using MLEM.Ui.Elements;
using MLEM.Ui.Style;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;
using TranSimCS.Worlds;

namespace TranSimCS.Menus {
    internal static class UI {
        public static Checkbox CreateCheck(InGameMenu menu, Element container, string name, string icon, Color? checkColor = null, Color? uncheckColor = null) {
            var check = new Checkbox(Anchor.AutoInline, new(21, 21), "", false);
            check.AddTooltip(name);
            check.Checkmark = LoadStyleProp(menu, icon);
            check.UncheckColor = uncheckColor ?? Color.Gray;
            check.CheckColor = checkColor ?? Color.White;
            container.AddChild(check);
            return check;
        }
        public static RadioButton CreateRadio(InGameMenu menu, Element container, string name, string icon, Action onSelected, string group = "") {
            var radio = new RadioButton(Anchor.AutoInline, new(21, 21), "", false, group);
            radio.OnCheckStateChange += (s, e) => {
                if (radio.Checked) onSelected();
            };
            radio.AddTooltip(name);
            radio.Checkmark = LoadStyleProp(menu, icon);
            container.AddChild(radio);
            return radio;
        }
        public static RadioButton CreateRadio<T>(InGameMenu menu, Element container, string name, string icon, Property<T> prop, T value){
            RadioButton radio = CreateRadio(menu, container, name, icon, () => prop.Value = value);
            prop.ValueChanged += (s, e) => {
                if (Object.Equals(e.OldValue, value)) radio.Checked = false;
                if (Object.Equals(e.NewValue, value)) radio.Checked = true;
            };
            radio.Checked = Object.Equals(value, prop.Value);
            return radio;
        }

        public static StyleProp<TextureRegion> LoadStyleProp(InGameMenu menu, string name) {
            return new StyleProp<TextureRegion>(new TextureRegion(menu.Game.Content.Load<Texture2D>(name)));
        }

        public static TextField SetUpProp<T>(string title, Panel panel, Property<T> prop, Func<T, string> getter, Func<string, T> setter){
            var textfieldSize = new Vector2(0.5f, 20);
            Paragraph label = new Paragraph(Anchor.AutoLeft, 0.5f, title);
            panel.AddChild(label);
            TextField textfield = new TextField(Anchor.AutoInline, textfieldSize, null, null, getter(prop.Value));
            panel.AddChild(textfield);
            textfield.OnTextChange = (field, str) => setter(str);
            EventHandler<PropertyChangedEventArgs2<T>> handler = (sender, e) => textfield.SetText(getter(prop.Value));
            prop.ValueChanged += handler;
            return textfield;
        }
        public static TextField SetUpFloatProp(string title, Panel panel, Property<float> prop) {
            var textfield = SetUpProp<float>(title, panel, prop, f => f.ToString(), s => RoadConfigurator.GetNewFloat(s, prop.Value));
            return textfield;
        }
    }
}