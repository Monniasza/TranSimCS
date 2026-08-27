using System;
using System.Collections.Generic;
using System.IO;
using ImageMagick;

namespace TranSimCS.SilkNet {
    public static class TexturePipeline {
        private static readonly string TextureRoot = Path.Combine(Program.DataRoot, "textures");
        private static readonly Dictionary<string, TextureData> textures = [];

        /// <summary>
        /// Gets a cached texture image.
        /// The returned Image is owned by TexturePipeline and must not be disposed
        /// by the caller.
        /// </summary>
        public static TextureData GetTexture(string name) {
            var texturePath = Path.IsPathFullyQualified(name) ? name : Path.Combine(TextureRoot, name);
            texturePath = Path.GetFullPath(texturePath);
            if (textures.TryGetValue(texturePath, out var texture)) return texture;
            using var stream = File.OpenRead(texturePath);
            var image = new MagickImage(stream);
            image.DetermineBitDepth();
            image.DetermineColorType();
            return textures[texturePath] = new(image);
        }
    }
}
