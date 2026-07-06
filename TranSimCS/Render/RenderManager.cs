using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DotNet.Collections.Generic;
using LanguageExt;
using LanguageExt.Pipes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.Property;
using TranSimCS.Setting;
using static TranSimCS.Model.MeshUnroll;
using static TranSimCS.Render.RenderManager;

namespace TranSimCS.Render {
    public class RenderManager: IDisposable {
        public readonly Property<Camera> CameraProp;
        public readonly Property<Vector4> AmbientColor;
        public readonly GraphicsDevice gpu;
        public readonly CollectionPool<VertexBuffer> VertexBufferPool;
        public readonly CollectionPool<IndexBuffer> IndexBufferPool;
        public readonly CollectionPool<VertexBuffer> InstanceBufferPool;
        public Matrix WorldViewProjection { get; private set; }
        public Matrix World { get; private set; }
        public Matrix View { get; private set; }
        public Matrix Projection { get; private set; }

        //CACHE, managed by RenderManager
        internal class MeshGPU{
            public int UploadedVersion;
            public VertexBuffer VB;
            public IndexBuffer IB;
            public void Dispose(RenderManager rm) {
                rm.VertexBufferPool.Return(VB);
                rm.IndexBufferPool.Return(IB);
                VB = null;
                IB = null;
                UploadedVersion = int.MaxValue;
            }
        }
        private readonly Dictionary<Mesh, MeshGPU> MeshCache = [];
        internal MeshGPU GetCachedMesh(Mesh mesh) {
            void CheckAndRebuild(MeshGPU meshGPU) {
                if (meshGPU.VB != null && meshGPU.IB != null && meshGPU.UploadedVersion == mesh.GeometryVersion) return;
                var vertexSize = mesh.Vertices.Count;
                var indexSize = mesh.Indices.Count;

                if (meshGPU.VB == null || meshGPU.VB.VertexCount < vertexSize) {
                    if (meshGPU.VB != null) VertexBufferPool.Return(meshGPU.VB);
                    meshGPU.VB = VertexBufferPool.Rent(vertexSize);
                }
                meshGPU.VB.SetData(mesh.Vertices.ToArray());

                if(meshGPU.IB == null || meshGPU.IB.IndexCount < indexSize){
                    if (meshGPU.IB != null) IndexBufferPool.Return(meshGPU.IB);
                    meshGPU.IB = IndexBufferPool.Rent(indexSize);
                }
                meshGPU.IB.SetData(mesh.Indices.ToArray());
                meshGPU.UploadedVersion = mesh.GeometryVersion;
            }

            if(MeshCache.TryGetValue(mesh, out var cache)){
                //Check if the cache needs a rebuild
                CheckAndRebuild(cache);
                return cache;
            }
            var cache2 = new MeshGPU();
            CheckAndRebuild(cache2);
            MeshCache[mesh] = cache2;
            return cache2;
        }
        internal void MeshCleanup(MultiMapList<Mesh, MeshDrawInstance> meshDrawInstances) {
            //Runs periodically to clean up the mesh cache to stop accumulating unnecessary meshes
            var uniqueMeshes = meshDrawInstances.Keys;

            List<Mesh> deleteCachesFor = [];
            foreach (var row in MeshCache) {
                var mesh = row.Key;
                if (meshDrawInstances.ContainsKey(mesh)) continue; //Don't delete caches for used meshes
                deleteCachesFor.Add(mesh);
            }
            foreach (var mesh in deleteCachesFor) {
                var meshGPU = MeshCache[mesh];
                meshGPU.Dispose(this);
            }
        }

        public RenderManager(GraphicsDevice gpu) {
            this.gpu = gpu;
            CameraProp = new(Camera.Default, "camera", null);
            AmbientColor = new(Vector4.One, "ambientColor", null);
            CameraProp.ValueChanged += (s, e) => SetUpEffects();
            SetUpEffects();
            VertexBufferPool = new(
                x => new VertexBuffer(gpu, typeof(VertexPositionColorTexture), x, BufferUsage.WriteOnly),
                x => x.Dispose(),
                x => x.VertexCount,
            128);
            IndexBufferPool = new(
                x => new IndexBuffer(gpu, typeof(ushort), x, BufferUsage.WriteOnly),
                x => x.Dispose(),
                x => x.IndexCount,
            384);
            InstanceBufferPool = new(
                x => new VertexBuffer(gpu, typeof(TransformQ), x, BufferUsage.WriteOnly),
                x => x.Dispose(),
                x => x.VertexCount,
            128);
        }
        private void SetUpEffects() {
            WorldViewProjection = Camera.GetCombinedMatrix(gpu, out var world, out var view, out var projection);
            World = world;
            View = view;
            Projection = projection;
        }

        public Camera Camera { get => CameraProp.Value; set => CameraProp.Value = value; }

        // Scratch arrays reused across frames to avoid per-frame allocations in Render()
        public static int GrowCapacity(int current, int needed) {
            // Grow exponentially to reduce the number of resizes
            int newCap = current == 0 ? 4 : current;
            while (newCap < needed) newCap = newCap * 2;
            return newCap;
        }


        public void Render(MultiMesh source) {
            //CONSTANTS
            var shader = Assets.ShaderEffect;
            var writeDepth = DepthStencilState.Default;
            var keepDepth = DepthStencilState.DepthRead;
            gpu.SamplerStates[0] = SamplerState.PointWrap;
            gpu.SamplerStates[1] = SamplerState.PointWrap;
            gpu.RasterizerState = Settings.InvertAllNormals ? RasterizerState.CullClockwise : RasterizerState.CullCounterClockwise;
            shader.Parameters["AmbientColor"].SetValue(AmbientColor.Value);
            shader.Parameters["WorldViewProjection"].SetValue(WorldViewProjection);

            //CATEGORIZATION & COUNTING
            int TriCount = 0;
            int VertCount = 0;
            var categorizedMeshes = new List<MeshDrawInstance>[(int)MaterialBlendMode.Count];
            foreach (var mdi in MeshTraversal.Traverse(source)) {
                var renderBin = mdi.Mesh;
                var renderPassID = mdi.Material.BlendMode;

                var bin = categorizedMeshes[(int)renderPassID] ??= new();
                bin.Add(mdi);

                TriCount += renderBin.Indices.Count / 3;
                VertCount += renderBin.Vertices.Count;
            }

            // PASS 0: SKYBOX
            // - Writes depth.
            // - No blending.
            // - Fills the depth buffer for the rest of the frame.


            // PASS 1: OPAQUE
            // - Also writes depth.
            // - Should discard transparent texels (requires a custom shader;
            //   BasicEffect cannot perform alpha testing).
            // - Since it writes depth, it behaves like opaque geometry.
            RenderPass(
                categorizedMeshes[(int)MaterialBlendMode.Opaque],
                writeDepth,
                BlendState.Opaque);
            RenderPass(
                categorizedMeshes[(int)MaterialBlendMode.Cutout],
                writeDepth,
                BlendState.Opaque);

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

        private void RenderPass(List<MeshDrawInstance>? bucket, DepthStencilState depthState, BlendState blendState, float alphaCutoff = 0.5f) {
            if (bucket == null || bucket.Count == 0) return;

            var shader = Assets.ShaderEffect;

            //Bind per-pass attributes
            gpu.BlendState = blendState;
            gpu.DepthStencilState = depthState;
            shader.Parameters["AlphaCutoff"].SetValue(alphaCutoff);

            //Group meshes by mesh
            var groupedMeshes = bucket.QuickGroup(x => x.Mesh);
            foreach (var meshGroup in groupedMeshes) {
                var mesh = meshGroup.Key;
                var instances = meshGroup.Value;
                if(instances.Count == 0 || mesh.Vertices.Count == 0 || mesh.Indices.Count == 0) continue;

                //Bind per-mesh
                var meshGPU = GetCachedMesh(mesh);
                gpu.Indices = meshGPU.IB;

                //For each material
                var groupedByMaterial = instances.QuickGroup(x => x.Material);
                foreach (var materialGroup in groupedByMaterial) {
                    var material = materialGroup.Key;
                    var materialInstances = materialGroup.Value;

                    if (materialInstances.Count == 0) continue;
                    var positionValues = materialInstances.Select(x => x.Transform).ToArray();

                    shader.Parameters["Albedo"].SetValue(material.Texture);
                    shader.Parameters["Emissive"].SetValue(material.Emissive);
                    shader.Parameters["EmissiveIsMask"].SetValue(material.EmissiveIsMask);

                    //Bind buffers
                    using (var instanceBufferRental = InstanceBufferPool.RentAsDisposable(positionValues.Length)) {
                        var instanceBuffer = instanceBufferRental.Value;
                        instanceBuffer.SetData(positionValues);
                        gpu.SetVertexBuffers(
                            new VertexBufferBinding(meshGPU.VB, 0, 0),
                            new VertexBufferBinding(instanceBuffer, 0, 1)
                        );

                        foreach (var pass in shader.CurrentTechnique.Passes) {
                            pass.Apply();
                            gpu.DrawInstancedPrimitives(
                                PrimitiveType.TriangleList,
                                baseVertex: 0,
                                startIndex: 0,
                                primitiveCount: mesh.Indices.Count / 3,
                                instanceCount: positionValues.Length
                            );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Releases system resources held by this RenderManager
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Dispose() {
            GC.SuppressFinalize(this);

            //Destroy caches
            foreach(var row in MeshCache) 
                row.Value.Dispose(this);
            MeshCache.Clear();

            //Destroy pools
            VertexBufferPool.Dispose();
            IndexBufferPool.Dispose();
            InstanceBufferPool.Dispose();
        }
    }
}
