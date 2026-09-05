using System.Numerics;
using System.Runtime.InteropServices;

namespace TranSimCS.Terrain {
    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainSample {
        /// <summary>
        /// A terrain sample position in respect to bounds of a <see cref="TerrainChunk"/>
        /// </summary>
        public Vector2 Position;
        public TerrainSample(Vector2 position) {
            Position = position;
        }
    }
}
