using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.SilkNet {
    public static class Texture2DConverter {
        private static readonly Dictionary<Texture2D, TextureData> conversions = [];
        public static TextureData Convert(this Texture2D texture) {
            if (conversions.TryGetValue(texture, out var cache)) return cache;
            var dumpedData = FromColorTexture(texture);
            conversions[texture] = dumpedData;
            return dumpedData;
        }

        private static TextureData FromColorTexture(Texture2D texture) {
            var pixels = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
            texture.GetData(pixels);

            var data = new byte[pixels.Length * 4];

            for (int i = 0; i < pixels.Length; i++) {
                var c = pixels[i];

                data[i * 4 + 0] = c.R;
                data[i * 4 + 1] = c.G;
                data[i * 4 + 2] = c.B;
                data[i * 4 + 3] = c.A;
            }

            return new TextureData(
                (uint)texture.Width,
                (uint)texture.Height,
                TextureFormat.RGBA8,
                data);
        }
    }
}
