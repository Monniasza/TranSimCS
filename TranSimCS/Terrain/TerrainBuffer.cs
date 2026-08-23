using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Geometry;

namespace TranSimCS.Terrain {
    public struct TerrainBuffer{
        public Texture2D HeightMap;
        public Texture2D TerrainTexture;
        public Vector4 MinMaxXZBounds;
        public Vector4 MinMaxUVBounds;
        public Vector2 HeightBounds;

        /// <summary>
        /// Generates a set of terrain chunks
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="frustum"></param>
        /// <param name="angularResolution"></param>
        /// <returns></returns>
        public IEnumerable<TerrainChunk> GenerateChunks(Camera camera, BoundingFrustum frustum, float angularResolution = 0.001f) {
            //Recursively subdivide the terrain into chunks
            Queue<TerrainChunk> chunks = new Queue<TerrainChunk>();
            void SubdivideChunk(TerrainChunk chunk) {
                float minX = chunk.MinPosition.X;
                float minY = chunk.MinPosition.Y;
                float minU = chunk.MinUV.X;
                float minV = chunk.MinUV.Y;

                float maxX = chunk.MaxPosition.X;
                float maxY = chunk.MaxPosition.Y;
                float maxU = chunk.MaxUV.X;
                float maxV = chunk.MaxUV.Y;

                float midX = (minX + maxX) / 2;
                float midY = (minY + maxY) / 2;
                float midU = (minU + maxU) / 2;
                float midV = (minV + maxV) / 2;

                TerrainChunk tc0 = new(minX, minY, minU, minV, midX, midY, midU, midV);
                TerrainChunk tc1 = new(midX, minY, midU, minV, maxX, midY, maxU, midV);
                TerrainChunk tc2 = new(minX, midY, minU, midV, midX, maxY, midU, maxV);
                TerrainChunk tc3 = new(midX, midY, midU, midV, maxX, maxY, maxU, maxV);

                chunks.Enqueue(tc0);
                chunks.Enqueue(tc1);
                chunks.Enqueue(tc2);
                chunks.Enqueue(tc3);
            }

            TerrainChunk terrainChunk = new TerrainChunk(MinMaxXZBounds, MinMaxUVBounds);

            while (chunks.Count > 0) {
                var chunk = chunks.Dequeue();
                var bounds = chunk.GenerateBounds(HeightBounds);
                var intersects = frustum.Intersects(bounds);
                if (!intersects) {
                    //Reject
                    continue;
                }

                var diagonal = Vector2.Distance(chunk.MinPosition, chunk.MaxPosition);
                var fov = 2 / frustum.Matrix.M11;
                var chunkCenter = (chunk.MinPosition + chunk.MaxPosition) / 2;
                var distanceToCenter = GeometryUtils.hypot2(camera.Position.X - chunkCenter.X, camera.Position.Z - chunkCenter.Y);
                var actualResolution = diagonal / (fov * distanceToCenter * 128);
                if(actualResolution > angularResolution) {
                    //Subdivide
                    SubdivideChunk(chunk);
                    continue;
                }

                //Emit
                yield return chunk;
            }
        }
    }
}
