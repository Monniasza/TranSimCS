using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Roads {
    public enum Surface {
        None = 0,
        Asphalt = 1,
        Dirt = 2,
        Concrete = 3,
        Tiles = 4,
        Cobble = 5,
    }
    public static class Surfaces {
        public static Texture2D? GetTexture(this Surface surface) {
            switch (surface) {
                case Surface.None:
                    return null;
                case Surface.Asphalt:
                    return Assets.Asphalt;
                case Surface.Dirt:
                    return Assets.Grass;
                case Surface.Concrete:
                    return Assets.Concrete;
                case Surface.Tiles:
                    return Assets.Tiles;
                case Surface.Cobble:
                    return Assets.Cobble;
                default:
                    throw new ArgumentException($"Unknown surface: {surface}");
            }
        }
    }

    public struct RoadFinish {
        public Surface subsurface;
        public float angle;
        public float depth;

        public RoadFinish(Surface subsurface, float angle, float depth) {
            this.subsurface = subsurface;
            this.angle = angle;
            this.depth = depth;
        }

        public static readonly RoadFinish None = new RoadFinish(Surface.None, 0, 0);
        public static readonly RoadFinish Embankment = new RoadFinish(Surface.Dirt, MathF.PI / 4, 1000);
        public static readonly RoadFinish Deck = new RoadFinish(Surface.Concrete, MathF.PI / 2, 1);
        public static readonly RoadFinish Wall = new RoadFinish(Surface.Concrete, MathF.PI / 2, 1000);
    }
}
