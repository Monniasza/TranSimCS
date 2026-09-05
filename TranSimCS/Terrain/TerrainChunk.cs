using System.Numerics;
using System.Runtime.InteropServices;
using TranSimCS.Geometry;

namespace TranSimCS.Terrain {
    [StructLayout(LayoutKind.Explicit)]
    public struct TerrainChunk{
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
        public AABB GenerateBounds(Vector2 heightBounds) {
            float minX = MinPosition.X;
            float minY = heightBounds.X;
            float minZ = MinPosition.Y;

            float maxX = MaxPosition.X;
            float maxY = heightBounds.Y;
            float maxZ = MaxPosition.Y;

            return new AABB(new(minX, minY, minZ), new(maxX, maxY, maxZ));
        }
    }
}
