using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.ModelOld {
    /// <summary>
    /// A material for <see cref="SimpleVertexProcessor{TVertex}"/>
    /// </summary>
    public struct SimpleMaterial : IEquatable<SimpleMaterial> {
        /// <summary>
        /// The texture used by the renderer. null for no texturing
        /// </summary>
        public Texture2D Texture = Assets.WhiteTex;
        public Texture2D Emissive = Assets.Black;
        public MaterialBlendMode BlendMode = MaterialBlendMode.Opaque;
        public float EmissiveIsMask = 0;

        public string TextureName { set => Texture = Assets.Content.Load<Texture2D>(value); }
        public string EmissiveName { set => Emissive = Assets.Content.Load<Texture2D>(value); }

        public SimpleMaterial() { }
        public SimpleMaterial(string texture = "white", MaterialBlendMode blendMode = MaterialBlendMode.Opaque) {
            TextureName = texture;
            BlendMode = blendMode;
        }
        public SimpleMaterial(string texture = "white") {
            TextureName = texture;
        }
        public static SimpleMaterial NewEmissive(string texture, MaterialBlendMode blendMode = MaterialBlendMode.Opaque) {
            return new SimpleMaterial() {
                EmissiveIsMask = 1,
                EmissiveName = texture,
                BlendMode = blendMode,
                Texture = Assets.Black
            };
        }

        public override bool Equals(object? obj)
            => obj is SimpleMaterial material && Equals(material);     
        
        public bool Equals(SimpleMaterial material) =>
            material.Texture == Texture
            && material.Emissive == Emissive
            && material.BlendMode == BlendMode
            && material.EmissiveIsMask == EmissiveIsMask;
        

        public override int GetHashCode() {
            return HashCode.Combine(Texture, Emissive, BlendMode, EmissiveIsMask);
        }

        public static bool operator ==(SimpleMaterial left, SimpleMaterial right) {
            return left.Equals(right);
        }

        public static bool operator !=(SimpleMaterial left, SimpleMaterial right) {
            return !(left == right);
        }
    }

    public enum MaterialBlendMode {
        /// <summary>
        /// Fully opaque. Fastest, disregards alpha
        /// </summary>
        Opaque = 0,      // fastest, no transparency
        /// <summary>
        /// Cut-out masks. Suitable for road markings
        /// </summary>
        Cutout = 1,      // alpha test style (arrows, decals)
        /// <summary>
        /// Transparent. Blends alpha. Slow due to need to sort
        /// </summary>
        Transparent = 2, // smooth blending
        /// <summary>
        /// Additive. Adds on top of other values
        /// </summary>
        Additive = 3,     // moderate, suitable for light (glare, lamps, flashes)
    
        /// <summary>
        /// The number of valid <see cref="MaterialBlendMode"/> values. It is not a valid value.
        /// </summary>
        Count = 4
    }
}
