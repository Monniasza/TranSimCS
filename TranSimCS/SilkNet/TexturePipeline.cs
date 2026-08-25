using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;

namespace TranSimCS.SilkNet {
    public static class TexturePipeline {
        private static readonly string TextureRoot = Path.Combine(Program.DataRoot, "textures");
        private static readonly Dictionary<string, Image> textures = [];

        /// <summary>
        /// Gets a cached texture image.
        /// The returned Image is owned by TexturePipeline and must not be disposed
        /// by the caller.
        /// </summary>
        public static Image GetTexture(string name) {
            var texturePath = Path.IsPathFullyQualified(name) ? name : Path.Combine(TextureRoot, name);
            texturePath = Path.GetFullPath(texturePath);
            if (textures.TryGetValue(texturePath, out var texture)) return texture;
            var image = Image.Load(texturePath);
            textures[texturePath] = image;
            return image;
        }
    }
}
