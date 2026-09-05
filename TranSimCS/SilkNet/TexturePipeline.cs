using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ImageMagick;

namespace TranSimCS.SilkNet {
    public static class TexturePipeline {
        private static readonly string TextureRoot = Path.Combine(Program.DataRoot, "Files/textures");
        private static readonly Dictionary<string, TextureData> textures = [];

        /// <summary>
        /// Gets a cached texture image.
        /// The returned Image is owned by TexturePipeline and must not be disposed
        /// by the caller.
        /// </summary>
        public static TextureData GetTexture(string name) {
            var texturePath = Path.IsPathFullyQualified(name) ? name : Path.Combine(TextureRoot, name);
            texturePath = Path.GetFullPath(texturePath);
            if (textures.TryGetValue(texturePath, out var texture)) {
                Debug.Assert(texture != null, "Cache contains a null texture");
                return texture;
            }
            using var stream = File.OpenRead(texturePath);
            var image = new MagickImage(stream);
            image.DetermineBitDepth();
            image.DetermineColorType();

            var loadedTexture = new TextureData(image);
            textures[texturePath] = loadedTexture;
            Debug.Assert(loadedTexture != null, "Failed to load a texture");
            return loadedTexture;
        }
    }
}
