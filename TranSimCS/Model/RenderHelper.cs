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

        public void Render() {
            int TriCount = 0;
            int VertCount = 0;
            GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
            foreach (var row in RenderBins) {
                var renderBin = row.Value;
                var texture = row.Key;
                Effect.Texture = texture;
                TriCount += renderBin.Indices.Count / 3;
                VertCount += renderBin.Vertices.Count;
                if (renderBin.Vertices.Count == 0 || renderBin.Indices.Count == 0) continue;
                foreach (var pass in Effect.CurrentTechnique.Passes) {
                    pass.Apply();
                    GraphicsDevice.SetVertexBuffer(new VertexBuffer(GraphicsDevice, typeof(VertexPositionColorTexture), renderBin.Vertices.Count, BufferUsage.WriteOnly));
                    GraphicsDevice.Indices = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, renderBin.Indices.Count, BufferUsage.WriteOnly);
                    GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, renderBin.Vertices.ToArray(), 0, renderBin.Vertices.Count, renderBin.Indices.ToArray(), 0, renderBin.Indices.Count / 3);
                }
            }
        }
    }
}
