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
        public MaterialBlendMode BlendMode = MaterialBlendMode.Opaque;

        public string TextureName { set => Texture = Assets.Content.Load<Texture2D>(value); }

        public SimpleMaterial() { }
        public SimpleMaterial(string texture, MaterialBlendMode blendMode = MaterialBlendMode.Opaque) {
            TextureName = texture;
            BlendMode = blendMode;
        }

        public override bool Equals(object? obj)
            => obj is SimpleMaterial material && Equals(material);     
        
        public bool Equals(SimpleMaterial material) 
        => EqualityComparer<Texture2D?>.Default.Equals(Texture, material.Texture) &&
           BlendMode == material.BlendMode;
        

        public override int GetHashCode() {
            return HashCode.Combine(Texture, BlendMode);
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
