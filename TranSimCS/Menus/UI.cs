using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Textures;
using MLEM.Ui;
using MLEM.Ui.Elements;
using MLEM.Ui.Style;
using TranSimCS.Menus.InGame;
using TranSimCS.Property;
using TranSimCS.Roads;
using TranSimCS.Tools;

namespace TranSimCS.Menus {
    internal static class UI {
        public static void AddProperty(this Checkbox check, Property<bool> prop) {
            check.Checked = prop.Value;
            check.OnCheckStateChange += (cb, isChecked) => prop.Value = isChecked;
            prop.ValueChanged += (s, old, val) => check.Checked = val;
        }
        public static Checkbox CreateCheck(InGameMenu menu, Element container, string name, string? icon = null, Color? checkColor = null, Color? uncheckColor = null) {
            var check = new Checkbox(Anchor.AutoInline, new(21, 21), "", false);
            check.AddTooltip(name);
            check.Checkmark = LoadStyleProp(menu, icon ?? Assets.CrossIcon);
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
            prop.ValueChanged += (s, old, val) => {
                if (Object.Equals(old, value)) radio.Checked = false;
                if (Object.Equals(val, value)) radio.Checked = true;
            };
            radio.Checked = Object.Equals(value, prop.Value);
            return radio;
        }

        public static StyleProp<TextureRegion> LoadStyleProp(InGameMenu menu, string name) {
            return new StyleProp<TextureRegion>(new TextureRegion(menu.Game.Content.Load<Texture2D>(name)));
        }
        public static void SetUpProp(string title, Panel panel, Element component) {
            var textfieldSize = new Vector2(0.5f, 20);
            Paragraph label = new Paragraph(Anchor.AutoLeft, 0.5f, title);
            panel.AddChild(label);
            component.Anchor = Anchor.AutoInline;
            component.Size = textfieldSize;
            panel.AddChild(component);
        }
        public static NumberField SetUpFloatProp(string title, Panel panel, Property<float>? prop) {
            var textfieldSize = new Vector2(0.5f, 20);
            var textfield = new NumberField(Anchor.AutoInline, textfieldSize, null, prop?.Value ?? 0);
            if (prop != null) {
                textfield.ValueChanged += (c, v) => prop.Value = v;
                prop.ValueChanged += (s, old, val) => textfield.Value = val;
            }
            SetUpProp(title, panel, textfield);
            return textfield;
        }
        public static Image.TextureCallback CreateTextureCallback(Texture2D texture2D) {
            return (_) => new MLEM.Textures.TextureRegion(texture2D);
        }
        public static Image.TextureCallback CreateTextureCallback(string name) => CreateTextureCallback(Assets.Content.Load<Texture2D>(name));
    }
}