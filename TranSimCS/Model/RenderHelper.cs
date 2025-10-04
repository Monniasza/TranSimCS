using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Model {
    public class RenderHelper: MultiMesh {
        public GraphicsDevice GraphicsDevice { get; private init; }
        public BasicEffect Effect { get; private init; }

        // Scratch arrays reused across frames to avoid per-frame allocations in Render()
        private VertexPositionColorTexture[] _vertexScratch = Array.Empty<VertexPositionColorTexture>();
        private int[] _indexScratch = Array.Empty<int>();

        public RenderHelper(GraphicsDevice graphicsDevice) {
            GraphicsDevice = graphicsDevice;
            Effect = new BasicEffect(graphicsDevice) {
                VertexColorEnabled = true,
                TextureEnabled = false,
                LightingEnabled = false
            };
        }
        public RenderHelper(GraphicsDevice graphicsDevice, BasicEffect effect) {
            GraphicsDevice = graphicsDevice;
            Effect = effect ?? throw new ArgumentNullException(nameof(effect), "Effect cannot be null. Please provide a valid BasicEffect instance.");
        }

        private static int GrowCapacity(int current, int needed) {
            // Grow exponentially to reduce the number of resizes
            int newCap = current == 0 ? 4 : current;
            while (newCap < needed) newCap = newCap * 2;
            return newCap;
        }
        private void EnsureScratchCapacity(int vertexCount, int indexCount) {
            if (_vertexScratch.Length < vertexCount) {
                Array.Resize(ref _vertexScratch, GrowCapacity(_vertexScratch.Length, vertexCount));
            }
            if (_indexScratch.Length < indexCount) {
                Array.Resize(ref _indexScratch, GrowCapacity(_indexScratch.Length, indexCount));
            }
        }

        public void Render() {
            int TriCount = 0;
            int VertCount = 0;

            // Ensure depth testing is enabled to prevent Z-fighting and flickering
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
            foreach (var row in RenderBins) {
                var renderBin = row.Value;
                var texture = row.Key;
                Effect.Texture = texture;
                TriCount += renderBin.Indices.Count / 3;
                VertCount += renderBin.Vertices.Count;
                if (renderBin.Vertices.Count == 0 || renderBin.Indices.Count == 0) continue;

                // Ensure pooled arrays are large enough, then copy list contents without allocating
                EnsureScratchCapacity(renderBin.Vertices.Count, renderBin.Indices.Count);
                renderBin.Vertices.CopyTo(_vertexScratch, 0);
                renderBin.Indices.CopyTo(_indexScratch, 0);

                foreach (var pass in Effect.CurrentTechnique.Passes) {
                    pass.Apply();
                    GraphicsDevice.DrawUserIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        _vertexScratch, 0, renderBin.Vertices.Count,
                        _indexScratch, 0, renderBin.Indices.Count / 3);
                }
            }
            
        }
    }
}
