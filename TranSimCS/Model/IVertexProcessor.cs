using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Save2;

namespace TranSimCS.Model {
    public interface IVertexProcessor<TMaterial, TVertex> {
        public void Render(RenderManager gpu, MeshElement<TMaterial, TVertex> mesh);
        public Vector3 GetVertexCoords(TVertex vertex);
    }
    public class VertexProcessor<TMaterial, TVertex>: IVertexProcessor<TMaterial, TVertex> {
        public readonly Action<RenderManager, MeshElement<TMaterial, TVertex>> Renderer;
        public readonly Func<TVertex, Vector3> CoordGetter;
        public VertexProcessor(Action<RenderManager, MeshElement<TMaterial, TVertex>> renderer, Func<TVertex, Vector3> coordGetter) {
            Renderer = renderer;
            CoordGetter = coordGetter;
        }
        public static IVertexProcessor<TMaterial, TVertex>? Default { get; set; }
        internal static IVertexProcessor<TMaterial, TVertex> GetDefault() => Default ?? throw new NotSupportedException($"Vertex processor for {typeof(TMaterial)}/{typeof(TVertex)} not supported");

        public Vector3 GetVertexCoords(TVertex vertex) => CoordGetter(vertex);

        public void Render(RenderManager gpu, MeshElement<TMaterial, TVertex> mesh) => Renderer(gpu, mesh);
    }
    public static class VertexProcessors {
        public static SimpleVertexProcessor<VertexPositionColorTexture> vpVPCT;

        static VertexProcessors() {
            vpVPCT = SimpleVertexProcessor<VertexPositionColorTexture>.INSTANCE;
        }
        /// <summary>
        /// Inithilizes this class
        /// </summary>
        public static void Init() { }
    }

    /// <summary>
    /// Implementation of <see cref="IVertexProcessor{TMaterial, TVertex}"/> for <see cref="SimpleMaterial"/> materials and vertex types implementing <see cref="IVertexType"/>
    /// </summary>
    /// <typeparam name="TVertex"></typeparam>
    public class SimpleVertexProcessor<TVertex> : IVertexProcessor<SimpleMaterial, TVertex> where TVertex : struct, IVertexType{
        public static readonly SimpleVertexProcessor<TVertex> INSTANCE;

        public Vector3 GetVertexCoords(TVertex vertex) => CoordGetter(vertex);

        public void Render(RenderManager gpu, MeshElement<SimpleMaterial, TVertex> mesh) {
            var mat = mesh.Material;

            //Copy data over to scratch arrays
            var verts = mesh.Vertices;
            var tris = mesh.Triangles;
            EnsureScratchCapacity(verts.Length, tris.Length * 3);
            Array.Copy(verts, _vertexScratch, verts.Length);
            for (int i = 0; i < tris.Length; i++) {
                var tri = tris[i];
                _indexScratch[i * 3] = tri.A;
                _indexScratch[i * 3 + 1] = tri.B;
                _indexScratch[i * 3 + 2] = tri.C;
            }

            gpu.effect.LightingEnabled = UseNormals;
            gpu.effect.TextureEnabled = mat.Texture != null;
            gpu.effect.Texture = mat.Texture ?? gpu.effect.Texture;

            // Ensure depth testing is enabled to prevent Z-fighting and flickering
            gpu.gpu.DepthStencilState = DepthStencilState.Default;
            gpu.gpu.SamplerStates[0] = SamplerState.PointWrap;

            foreach (var pass in gpu.effect.CurrentTechnique.Passes) {
                pass.Apply();
                gpu.gpu.DrawUserIndexedPrimitives<TVertex>(
                    PrimitiveType.TriangleList,
                    _vertexScratch, 0, verts.Length,
                    _indexScratch, 0, tris.Length);
            }
        }

        // Scratch arrays reused across frames to avoid per-frame allocations in Render()
        private static TVertex[] _vertexScratch = [];
        private static int[] _indexScratch = [];
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


        public static Func<TVertex, Vector3> CoordGetter;
        private static Dictionary<GraphicsDevice, BasicEffect> effectGenerator = [];
        public static bool UseNormals;
        public static bool UseTextures;

        static SimpleVertexProcessor() {
            TVertex exemplar = new();
            var declaration = exemplar.VertexDeclaration;
            foreach(var element in declaration.GetVertexElements()) {
                switch (element.VertexElementUsage) {
                    case VertexElementUsage.Position:
                        var fieldOffset = element.Offset;
                        CoordGetter = JsonProcessor.CreateDelegate<TVertex, Vector3>(fieldOffset);
                        break;
                    case VertexElementUsage.Normal:
                        UseNormals = true;
                        break;
                    case VertexElementUsage.TextureCoordinate:
                        UseTextures = true;
                        break;
                }
            }
        }
    }
}
