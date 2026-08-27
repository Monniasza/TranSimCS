using System;
using System.Collections.Generic;
using System.IO;
using StbImageSharp;

namespace TranSimCS.SilkNet {
    public static class TexturePipeline {
        private static readonly string TextureRoot = Path.Combine(Program.DataRoot, "textures");
        private static readonly Dictionary<string, ImageResult> textures = [];

        /// <summary>
        /// Gets a cached texture image.
        /// The returned Image is owned by TexturePipeline and must not be disposed
        /// by the caller.
        /// </summary>
        public static ImageResult GetTexture(string name) {
            var texturePath = Path.IsPathFullyQualified(name) ? name : Path.Combine(TextureRoot, name);
            texturePath = Path.GetFullPath(texturePath);
            if (textures.TryGetValue(texturePath, out var texture)) return texture;
            using var stream = File.OpenRead(texturePath);
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            textures[texturePath] = image;
            return image;
        }
    }
}
