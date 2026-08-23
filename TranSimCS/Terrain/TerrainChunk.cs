using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Terrain {
    [StructLayout(LayoutKind.Explicit)]
    public struct TerrainChunk: IVertexType {
        [FieldOffset(0)]
        public float MinX;
        [FieldOffset(4)]
        public float MinZ;
        [FieldOffset(8)]
        public float MinU;
        [FieldOffset(12)]
        public float MinV;
        [FieldOffset(16)]
        public float MaxX;
        [FieldOffset(20)]
        public float MaxZ;
        [FieldOffset(24)]
        public float MaxU;
        [FieldOffset(28)]
        public float MaxV;

        [FieldOffset(0)]
        public Vector2 MinPosition;
        [FieldOffset(8)]
        public Vector2 MaxPosition;
        [FieldOffset(16)]
        public Vector2 MinUV;
        [FieldOffset(24)]
        public Vector2 MaxUV;

        [FieldOffset(0)]
        public Vector4 PositionRange;
        [FieldOffset(16)]
        public Vector4 TextureRange;

        private static VertexDeclaration _vertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1)
        );

        public TerrainChunk(Vector2 minPosition, Vector2 maxPosition, Vector2 minUV, Vector2 maxUV) : this() {
            MinPosition = minPosition;
            MaxPosition = maxPosition;
            MinUV = minUV;
            MaxUV = maxUV;
        }
        public TerrainChunk(Vector4 positionRange, Vector4 textureRange) : this() {
            PositionRange = positionRange;
            TextureRange = textureRange;
        }
        public TerrainChunk(float minX, float minZ, float minU, float minV, float maxX, float maxZ, float maxU, float maxV) : this() {
            MinX = minX;
            MinZ = minZ;
            MinU = minU;
            MinV = minV;
            MaxX = maxX;
            MaxZ = maxZ;
            MaxU = maxU;
            MaxV = maxV;
        }

        public VertexDeclaration VertexDeclaration => _vertexDeclaration;

        public BoundingBox GenerateBounds(Vector2 heightBounds) {
            float minX = MinPosition.X;
            float minY = heightBounds.X;
            float minZ = MinPosition.Y;

            float maxX = MaxPosition.X;
            float maxY = heightBounds.Y;
            float maxZ = MaxPosition.Y;

            return new BoundingBox(new(minX, minY, minZ), new(maxX, maxY, maxZ));
        }
    }
}
