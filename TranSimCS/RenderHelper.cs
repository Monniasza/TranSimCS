using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS {
    internal class RenderHelper {
        public GraphicsDevice GraphicsDevice { get; private init; }
        public BasicEffect Effect { get; private init; }

        public RenderHelper(GraphicsDevice graphicsDevice) {
            GraphicsDevice = graphicsDevice;
            Effect = new BasicEffect(graphicsDevice) {
                VertexColorEnabled = true,
                TextureEnabled = false,
                LightingEnabled = false
            };
        }
    }
}
