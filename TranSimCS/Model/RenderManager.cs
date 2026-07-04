using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.ModelOld;
using TranSimCS.Property;
using TranSimCS.Setting;

namespace TranSimCS.Model {
    public class RenderManager {
        public readonly Property<Camera> CameraProp;
        public readonly Property<Vector4> AmbientColor;
        public readonly GraphicsDevice gpu;

        public Matrix WorldViewProjection { get; private set; }
        public Matrix World { get; private set; }
        public Matrix View { get; private set; }
        public Matrix Projection { get; private set; }

        public RenderManager(GraphicsDevice gpu) {
            this.gpu = gpu;
            CameraProp = new(Camera.Default, "camera", null);
            AmbientColor = new(Vector4.One, "ambientColor", null);
            CameraProp.ValueChanged += (s, e) => SetUpEffects();
            SetUpEffects();
        }
        private void SetUpEffects() {
            WorldViewProjection = Camera.GetCombinedMatrix(gpu, out var world, out var view, out var projection);
            World = world;
            View = view;
            Projection = projection;
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

        public void Render(MultiMesh source) {
            MultiMesh mesh = new MultiMesh();
            MeshUnroll.Unroll(source, mesh);

            //CONSTANTS
            var writeDepth = DepthStencilState.Default;
            var keepDepth = DepthStencilState.DepthRead;
            gpu.SamplerStates[0] = SamplerState.PointWrap;
            gpu.SamplerStates[1] = SamplerState.PointWrap;

            //CATEGORIZATION & COUNTING
            int TriCount = 0;
            int VertCount = 0;
            var categorizedMeshes = new List<KeyValuePair<SimpleMaterial, Mesh>>[(int)MaterialBlendMode.Count];
            foreach (var row in mesh.RenderBins) {
                var renderBin = row.Value;
                var texture = row.Key;
                var renderPassID = texture.BlendMode;

                var bin = categorizedMeshes[(int)renderPassID] ??= new();
                bin.Add(row);

                var verts = renderBin.Vertices.Count;
                var tris = renderBin.Indices.Count / 3;
                TriCount += renderBin.Indices.Count / 3;
                VertCount += renderBin.Vertices.Count;
            }

            // PASS 0: OPAQUE
            // - Writes depth.
            // - No blending.
            // - Fills the depth buffer for the rest of the frame.
            RenderPass(
                categorizedMeshes[(int)MaterialBlendMode.Opaque],
                writeDepth,
                BlendState.Opaque);
            
            // PASS 1: CUTOUT
            // - Also writes depth.
            // - Should discard transparent texels (requires a custom shader;
            //   BasicEffect cannot perform alpha testing).
            // - Since it writes depth, it behaves like opaque geometry.
            RenderPass(
                categorizedMeshes[(int)MaterialBlendMode.Cutout],
                writeDepth,
                BlendState.Opaque, 0.5f);

            // PASS 2: ADDITIVE
            // - Reads depth so it is hidden by opaque/cutout geometry.
            // - Does not write depth so multiple additive effects can overlap.
            RenderPass(
                categorizedMeshes[(int)MaterialBlendMode.Additive],
                keepDepth,
                BlendState.Additive);

            // PASS 3: TRANSPARENT
            // - Reads depth.
            // - Does not write depth.
            // - Should ideally be drawn back-to-front within this bucket.
            RenderPass(
                categorizedMeshes[(int)MaterialBlendMode.Transparent],
                keepDepth,
                BlendState.AlphaBlend, 0.00001f);
        }

        private void RenderPass(List<KeyValuePair<SimpleMaterial, Mesh>>? bucket, DepthStencilState depthState, BlendState blendState, float alphaCutoff = 0.5f) {
            if (bucket == null || bucket.Count == 0) return;

            foreach (var row in bucket) {
                var renderBin = row.Value;
                var texture = row.Key;
                if (renderBin.Vertices.Count == 0 || renderBin.Indices.Count == 0) continue;

                //Create a set of RenderInputs
                RenderInputs renderInputs = new RenderInputs() {
                    Albedo = texture.Texture,
                    Emissive = texture.Emissive,
                    DepthStencilState = depthState,
                    BlendState = blendState,
                    WorldViewProjection = WorldViewProjection,
                    AmbientColor = AmbientColor.Value,
                    AlphaCutoff = alphaCutoff,
                    EmissiveIsMask = texture.EmissiveIsMask
                };
                RenderRow(renderBin, renderInputs);
            }
        }

        private void RenderRow(Mesh renderBin, RenderInputs renderInputs) {
            if (renderBin.Vertices.Count == 0 || renderBin.Indices.Count == 0) return;
            var eff = Assets.ShaderEffect;

            // Ensure pooled arrays are large enough, then copy list contents without allocating
            EnsureScratchCapacity(renderBin.Vertices.Count, renderBin.Indices.Count);
            renderBin.Indices.CopyTo(_indexScratch, 0);
            renderBin.Vertices.CopyTo(_vertexScratch, 0);

            //If requested, invert all normals
            if (Settings.InvertAllNormals) RenderUtil.InvertNormals(_indexScratch, renderBin.Indices.Count);

            foreach (var pass in eff.CurrentTechnique.Passes) {
                renderInputs.PassInputsToEffect(eff, gpu);
                pass.Apply();
                gpu.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _vertexScratch, 0, renderBin.Vertices.Count,
                    _indexScratch, 0, renderBin.Indices.Count / 3);
            }
        }
    }

    public struct RenderInputs {
        public Matrix WorldViewProjection = Matrix.Identity;
        public Vector4 AmbientColor = Vector4.One;
        public float AlphaCutoff = 0.5f;
        public float EmissiveIsMask = 0;
        public Texture Albedo;
        public Texture Emissive;
        public DepthStencilState DepthStencilState;
        public BlendState BlendState;
        public RenderInputs() { }
        public void PassInputsToEffect(Effect effect, GraphicsDevice gpu) {
            gpu.BlendState = BlendState;
            gpu.DepthStencilState = DepthStencilState;
            effect.Parameters["Albedo"].SetValue(Albedo);
            effect.Parameters["Emissive"].SetValue(Emissive);
            effect.Parameters["AmbientColor"].SetValue(AmbientColor);
            effect.Parameters["WorldViewProjection"].SetValue(WorldViewProjection);
            effect.Parameters["AlphaCutoff"].SetValue(AlphaCutoff);
            effect.Parameters["EmissiveIsMask"].SetValue(EmissiveIsMask);
        }
    }
}
