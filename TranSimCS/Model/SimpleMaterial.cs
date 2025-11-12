using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Model {
    /// <summary>
    /// A material for <see cref="SimpleVertexProcessor{TVertex}"/>
    /// </summary>
    public struct SimpleMaterial {
        /// <summary>
        /// The texture used by the renderer. null for no texturing
        /// </summary>
        public Texture2D? Texture;
    }
}
