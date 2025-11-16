using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Model;
using TranSimCS.Worlds.Property;

namespace TranSimCS.ModelOld {
    public class RenderManager {
        public readonly Property<Camera> CameraProp;
        public readonly GraphicsDevice gpu;
        public readonly BasicEffect effect;

        public RenderManager(GraphicsDevice gpu) {
            this.gpu = gpu;
            CameraProp = new(Camera.Default, "camera", null);
            effect = new BasicEffect(gpu);
            Camera.SetUpEffect(effect, gpu);
            CameraProp.ValueChanged += CameraProp_ValueChanged;
        }

        private void CameraProp_ValueChanged(object? sender, PropertyChangedEventArgs2<Camera> e) {
            Camera.SetUpEffect(effect, gpu);
        }

        public Camera Camera { get => CameraProp.Value; set => CameraProp.Value = value; }

        // Scratch arrays reused across frames to avoid per-frame allocations in Render()
        private VertexPositionColorTexture[] _vertexScratch = Array.Empty<VertexPositionColorTexture>();
        private int[] _indexScratch = Array.Empty<int>();
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

        public void Render(MultiMesh mesh) {
            int TriCount = 0;
            int VertCount = 0;
            
            effect.LightingEnabled = false;
            effect.TextureEnabled = true;
            effect.VertexColorEnabled = true;

            // Ensure depth testing is enabled to prevent Z-fighting and flickering
            gpu.DepthStencilState = DepthStencilState.Default;

            gpu.SamplerStates[0] = SamplerState.PointWrap;
            foreach (var row in mesh.RenderBins) {
                var renderBin = row.Value;
                var texture = row.Key;
                effect.Texture = texture;
                TriCount += renderBin.Indices.Count / 3;
                VertCount += renderBin.Vertices.Count;
                if (renderBin.Vertices.Count == 0 || renderBin.Indices.Count == 0) continue;

                // Ensure pooled arrays are large enough, then copy list contents without allocating
                EnsureScratchCapacity(renderBin.Vertices.Count, renderBin.Indices.Count);
                renderBin.Vertices.CopyTo(_vertexScratch, 0);
                renderBin.Indices.CopyTo(_indexScratch, 0);

                foreach (var pass in effect.CurrentTechnique.Passes) {
                    pass.Apply();
                    gpu.DrawUserIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        _vertexScratch, 0, renderBin.Vertices.Count,
                        _indexScratch, 0, renderBin.Indices.Count / 3);
                }
            }
        }
    }
}
