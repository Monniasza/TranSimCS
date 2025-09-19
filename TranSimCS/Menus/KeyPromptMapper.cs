using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using MLEM.Textures;

namespace TranSimCS.Menus {
    public static class KeyPromptMapper {
        private static ContentManager contentLoader;
        private static Dictionary<object, TextureRegion> keyPrompts = [];

        internal static void SetUpKeyPrompts(ContentManager content) {
            contentLoader = content;

            var hzCount = 16;
            var tileSize = new Point(23, 8);
            var tileTexture = content.Load<Texture2D>("ui/vk");

            for(int i = 0; i < 256; i++) {
                Keys key = (Keys)i;
                var tileIdX = i % hzCount;
                var tileIdY = i / hzCount;
                var tilePos = new Point(1 + tileIdX * 24, 1 + tileIdY * 9);
                TextureRegion tr = new(tileTexture, tilePos, tileSize);
                AddPrompt(key, tr);
            }
            (MouseButton, int)[] mousedata = [
                (MouseButton.Left, 0),
                (MouseButton.Middle, 3),
                (MouseButton.Right, 1),
                (MouseButton.Extra1, 4),
                (MouseButton.Extra2, 5),
            ];
            foreach (var key in mousedata) {
                var mouse = key.Item1;
                var i = key.Item2;
                var tileIdX = i % hzCount;
                var tileIdY = i / hzCount;
                var tilePos = new Point(1 + tileIdX * 24, 1 + tileIdY * 9);
                TextureRegion tr = new(tileTexture, tilePos, tileSize);
                AddPrompt(mouse, tr);
            }
        }

        public static void AddPrompt(object key, string pre, string path) => AddPrompt(key, pre + path);
        public static void AddPrompt(object key, string prompt) => AddPrompt(key, contentLoader.Load<Texture2D>(prompt));
        public static void AddPrompt(object key, Texture2D prompt) => AddPrompt(key, new TextureRegion(prompt));
        
        public static void AddPrompt(object key, TextureRegion prompt) {
            keyPrompts.Add(key, prompt);
        }

        internal static TextureRegion GetPrompt(object key) {
            TextureRegion texture = null;
            var success = keyPrompts.TryGetValue(key, out texture);
            if (success) return texture;
            return null;
        }
    }
}
