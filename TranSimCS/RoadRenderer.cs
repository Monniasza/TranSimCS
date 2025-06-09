using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS {
    internal static class RoadRenderer {
        public static void RenderRoadSegment(RoadNode node, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            // Example rendering logic for a road node
            // This is a placeholder and should be replaced with actual rendering code
            spriteBatch.Begin();
            spriteBatch.Draw(Texture2D.WhiteTexture, new Rectangle((int)node.Position.X, (int)node.Position.Z, 10, 10), Color.Red);
            spriteBatch.End();
        }
    }
}
