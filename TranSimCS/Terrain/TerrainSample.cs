using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Terrain {
    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainSample: IVertexType {
        /// <summary>
        /// A terrain sample position in respect to bounds of a <see cref="TerrainChunk"/>
        /// </summary>
        public Vector2 Position;
        public TerrainSample(Vector2 position) {
            Position = position;
        }

        private static VertexDeclaration _vertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0)
        );
        public VertexDeclaration VertexDeclaration => _vertexDeclaration;
    }
}
