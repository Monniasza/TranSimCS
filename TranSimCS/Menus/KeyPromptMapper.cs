using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;

namespace TranSimCS.Menus {
    public static class KeyPromptMapper {
        private static ContentManager contentLoader;
        private static Dictionary<object, Texture2D> keyPrompts = [];

        internal static void SetUpKeyPrompts(ContentManager content) {
            contentLoader = content;
            (object, string)[] keyPrompts = [
                (Keys.D0, "0"),
                (Keys.D1, "1"),
                (Keys.D2, "2"),
                (Keys.D3, "3"),
                (Keys.D4, "4"),
                (Keys.D5, "5"),
                (Keys.D6, "6"),
                (Keys.D7, "7"),
                (Keys.D8, "8"),
                (Keys.D9, "9"),
                (Keys.A, "a"),
                (Keys.LeftAlt, "alt"),
                (SpecialKey.Any, "any"),
                (Keys.OemQuotes, "apostrophe"),
                (Keys.Down, "arrow_down"),
                (Keys.Left, "arrow_left"),
                (Keys.Right, "arrow_right"),
                (Keys.Up, "arrow_up"),
                (Keys.B, "b"),
                (Keys.Back, "backspace_icon_alternative"),
                (Keys.OemCloseBrackets, "bracket_close"),
                (Keys.OemOpenBrackets, "bracket_open"),
                (Keys.C, "c"), (Keys.CapsLock, "capslock_icon"),
                (Keys.OemComma, "comma"),
                (Keys.LeftControl, "command"),
                (Keys.D, "d"),
                (Keys.Delete, "delete"),
                (Keys.E, "e"),
                (Keys.End, "end"),
                (Keys.Enter, "enter"),
                (Keys.OemPlus, "equals"),
                (Keys.Escape, "escape"),
                (Keys.F, "f"),
                (Keys.F1, "f1"),
                (Keys.F2, "f2"),
                (Keys.F3, "f3"),
                (Keys.F4, "f4"),
                (Keys.F5, "f5"),
                (Keys.F6, "f6"),
                (Keys.F7, "f7"),
                (Keys.F8, "f8"),
                (Keys.F9, "f9"),
                (Keys.F10, "f10"),
                (Keys.F11, "f11"),
                (Keys.F12, "f12"),
                (Keys.G, "g"),
                (Keys.H, "h"),
                (Keys.Home, "home"),
                (Keys.I, "i"),
                (Keys.Insert, "insert"),
                (Keys.J, "j"),
                (Keys.K, "k"),
                (Keys.L, "l"),
                (Keys.M,  "m"),
                (Keys.OemMinus, "minus"),
                (Keys.N, "n"),
                (Keys.NumLock, "numlock"),
                (Keys.O, "o"),
                (Keys.P, "p"),
                (Keys.PageDown, "page_down"),
                (Keys.PageUp, "page_up"),
                (Keys.OemPeriod, "period"),
                (Keys.PrintScreen, "printscreen"),
                (Keys.Q, "q"),
                (Keys.R, "r"),
                (Keys.S, "s"),
                (Keys.OemSemicolon, "semicolon"),
                (Keys.LeftShift, "shift"),
                (Keys.OemBackslash, "slash_back"),
                (Keys.OemQuestion, "slash_forward"),
                (Keys.Space, "space_icon"),
                (Keys.T, "t"),
                (Keys.Tab, "tab_icon"),
                (Keys.OemTilde, "tilde"),
                (Keys.U, "u"),
                (Keys.V, "v"),
                (Keys.W, "w"),
                (Keys.LeftWindows, "win"),
                (Keys.X, "x"),
                (Keys.Y, "y"),
                (Keys.Z, "z"),
            ];

            (object, string)[] mousePrompts = [
                (MouseButton.Left, "left"),
                (MouseButton.Right, "right"),
                (MouseButton.Middle, "scroll"),
                (SpecialKey.Horizontal, "vertical"),
                (SpecialKey.Vertical, "vertical"),
                (SpecialKey.Scroll, "scroll_vertical"),
                (SpecialKey.ScrollDown, "scroll_down"),
                (SpecialKey.ScrollUp, "scroll_up"),
                (SpecialKey.MouseMove, "move"),
            ];
            var keyPath = "kenney input prompts/Keyboard & Mouse/Default/keyboard_";
            var mousePath = "kenney input prompts/Keyboard & Mouse/Default/mouse_";
            foreach(var key in mousePrompts) AddPrompt(key.Item1, mousePath, key.Item2);
            foreach (var key in keyPrompts) AddPrompt(key.Item1, keyPath, key.Item2);
        }

        public static void AddPrompt(object key, string pre, string path) => AddPrompt(key, pre + path);
        public static void AddPrompt(object key, string prompt) => AddPrompt(key, contentLoader.Load<Texture2D>(prompt));
        public static void AddPrompt(object key, Texture2D prompt) {
            keyPrompts.Add(key, prompt);
        }

        internal static Texture2D GetPrompt(object key) {
            Texture2D texture = null;
            var success = keyPrompts.TryGetValue(key, out texture);
            if (success) return texture;
            return null;
        }
    }
}
