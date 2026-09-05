using System;
using TranSimCS.ModelOld;

namespace TranSimCS.Roads {
    public enum Surface {
        None = 0,
        Asphalt = 1,
        Dirt = 2,
        Concrete = 3,
        Tiles = 4,
        Cobble = 5,
        Grass = 6
    }
    public static class Surfaces {
        public static SimpleMaterial? GetTexture(this Surface surface) {
            switch (surface) {
                case Surface.None:
                    return null;
                case Surface.Asphalt:
                    return Materials.Asphalt;
                case Surface.Dirt:
                    return Materials.Grass;
                case Surface.Concrete:
                    return Materials.Concrete;
                case Surface.Tiles:
                    return Materials.Tiles;
                case Surface.Cobble:
                    return Materials.Cobble;
                case Surface.Grass:
                    return Materials.Grass;
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
        public static readonly RoadFinish Embankment = new RoadFinish(Surface.Dirt, MathF.PI / 4, 10);
        public static readonly RoadFinish Deck = new RoadFinish(Surface.Concrete, MathF.PI / 2, 1);
        public static readonly RoadFinish Wall = new RoadFinish(Surface.Concrete, MathF.PI / 2, 10);
    }
}
