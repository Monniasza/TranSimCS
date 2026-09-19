using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using LanguageExt.Pipes;
using Silk.NET.OpenGL;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.Property;
using TranSimCS.Setting;
using TranSimCS.Terrain;
using TranSimCS.Worlds;
using static TranSimCS.Model.MeshUnroll;

namespace TranSimCS.SilkNet {
    /// <summary>
    /// A source of geometry in form of <see cref="MeshDrawInstance"/>s. Takes a <see cref="RenderTarget"/> as the context.
    /// </summary>
    public delegate void GeometrySupplier(RenderTarget target);

    public struct RenderStats {
        public int VertexCount;
        public int TriangleCount;
        public int InstanceCount;
        public int MaterialCount;
        public int TagCount;
        public int ModelCount;
        public int DrawCount;
    }

    public class RenderManager: IDisposable {
        public readonly Property<Camera> CameraProp;
        public readonly Property<Vector4> AmbientColor;
        public readonly SilkNetTest window;

        public static readonly string MeshVertSource;
        public static readonly string FragSource;

        public Matrix4x4 WorldViewProjection { get; private set; }
        public Matrix4x4 World { get; private set; }
        public Matrix4x4 View { get; private set; }
        public Matrix4x4 Projection { get; private set; }
        public event GeometrySupplier? OnRender;
        
        internal uint _instanceBuffer;
        internal uint _vertexShader;
        internal uint _fragmentShader;
        internal uint _meshProgram;
        internal uint _uniformBuffer;

        
        static RenderManager() {
            MeshVertSource = TerrainDataBlobs.ReadEmbeddedResource("TranSimCS.Include.mesh.vert");
            FragSource = TerrainDataBlobs.ReadEmbeddedResource("TranSimCS.Include.mesh.frag");
        }


        private readonly Dictionary<Mesh, MeshGPU> MeshCache = [];
        internal MeshGPU GetCachedMesh(Mesh mesh) {
            if(MeshCache.TryGetValue(mesh, out var cache)){
                //Check if the cache needs a rebuild
                cache.CheckAndRefresh(mesh);
                return cache;
            }
            var cache2 = new MeshGPU(this);
            cache2.CheckAndRefresh(mesh);
            MeshCache[mesh] = cache2;
            return cache2;
        }


        private readonly Dictionary<TextureData, TextureGPU> TextureCache = [];
        internal TextureGPU GetCachedTexture(TextureData tex) {
            if (TextureCache.TryGetValue(tex, out var cache)) 
                return cache;
            var texGL = new TextureGPU(tex, window.OpenGL);
            TextureCache[tex] = texGL;
            return texGL;
        }

        internal void MeshCleanup(ISet<Mesh> meshDrawInstances) {
            //Runs periodically to clean up the mesh cache to stop accumulating unnecessary meshes
            List<Mesh> deleteCachesFor = [];
            foreach (var row in MeshCache) {
                var mesh = row.Key;
                if (meshDrawInstances.Contains(mesh)) continue; //Don't delete caches for used meshes
                deleteCachesFor.Add(mesh);
            }
            foreach (var mesh in deleteCachesFor) {
                var meshGPU = MeshCache[mesh];
                meshGPU.Dispose();
                MeshCache.Remove(mesh);
            }
        }

        public RenderManager(SilkNetTest gpu) {
            this.window = gpu;
            var gl = gpu.OpenGL;
            const int glTrue = (int)GLEnum.True;

            _instanceBuffer = gl.GenBuffer();
            _vertexShader = gl.CreateShader(ShaderType.VertexShader);
            _fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
            
            gl.ShaderSource(_vertexShader, MeshVertSource);
            gl.ShaderSource(_fragmentShader, FragSource);
            gl.CompileShader(_vertexShader);
            gl.CompileShader(_fragmentShader);
            gl.GetShader(_vertexShader, ShaderParameterName.CompileStatus, out int vertStatus);
            gl.GetShader(_fragmentShader, ShaderParameterName.CompileStatus, out int fragStatus);
            if (vertStatus != glTrue) throw new Exception("Failed to compile vertex shader: " + gl.GetShaderInfoLog(_vertexShader));
            if (fragStatus != glTrue) throw new Exception("Failed to compile fragment shader: " + gl.GetShaderInfoLog(_fragmentShader));

            _meshProgram = gl.CreateProgram();
            gl.AttachShader(_meshProgram, _vertexShader);
            gl.AttachShader(_meshProgram, _fragmentShader);
            gl.LinkProgram(_meshProgram);
            gl.GetProgram(_meshProgram, ProgramPropertyARB.LinkStatus, out int linkStatus);
            if (linkStatus != glTrue) throw new Exception("Failed to link mesh program: " + gl.GetProgramInfoLog(_meshProgram));


            _uniformBuffer = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.UniformBuffer, _uniformBuffer);
            const nuint uniformSize = 96;
            Debug.Assert((nuint)Unsafe.SizeOf<ShaderUniformData>() == uniformSize);
            ShaderUniformData[] fakeUniformData = null;
            gl.BufferData(BufferTargetARB.UniformBuffer, uniformSize, fakeUniformData, BufferUsageARB.DynamicDraw);
            gl.BindBufferBase(BufferTargetARB.UniformBuffer, 0, _uniformBuffer);

            CameraProp = new(Camera.Default, "camera", null);
            AmbientColor = new(Vector4.One, "ambientColor", null);
            CameraProp.ValueChanged += (s, old, value) => SetUpEffects();
            SetUpEffects();
        }
        private void SetUpEffects() {
            var windowDimensions = window.SilkWindow.Size;
            if (windowDimensions.X <= 0 || windowDimensions.Y <= 0) return;
            WorldViewProjection = Camera.GetCombinedMatrix(windowDimensions.X, windowDimensions.Y, out var world, out var view, out var projection);
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

        public RenderStats Stats { get; private set; }

        public void Render() {
            SetUpEffects();

            //CONSTANTS
            var gl = window.OpenGL;

            gl.Enable(EnableCap.DepthTest);
            gl.UseProgram(_meshProgram);
            CheckError("UseProgram");

            gl.Enable(EnableCap.DepthTest);
            gl.DepthFunc(DepthFunction.Less);
            gl.DepthMask(true);

            gl.CullFace(Settings.InvertAllNormals ? TriangleFace.Back : TriangleFace.Front);

            //CATEGORIZATION & COUNTING
            List<MeshDrawInstance> instances = [];
            OnRender?.Invoke(instances.Add);

            var stats = new RenderStats();

            var groups = new List<MeshDrawInstance>?[(int)MaterialBlendMode.Count];
            var uniqueMeshes = new HashSet<Mesh>();
            for(int i = 0; i < instances.Count; i++) {
                var instance = instances[i];
                var list = groups[(int)instance.Material.BlendMode] ??= [];
                list.Add(instance);
                uniqueMeshes.Add(instance.Mesh);
            }
            MeshCleanup(uniqueMeshes);

            //Bind per-pass attributes
            gl.Disable(EnableCap.Blend);
            RenderPass(groups[(int)MaterialBlendMode.Opaque], 0, ref stats);
            RenderPass(groups[(int)MaterialBlendMode.Cutout], 0.5f, ref stats);

            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            RenderPass(groups[(int)MaterialBlendMode.Transparent], 0, ref stats);

            Stats = stats;
        }

        private readonly List<List<MeshDrawInstance>> GroupListPool = [];
        private int GroupListPoolIndex;
        private readonly Dictionary<Mesh, List<MeshDrawInstance>> MeshGroupScratch = [];
        private readonly List<Mesh> MeshGroupOrder = [];
        private readonly Dictionary<SimpleMaterial, List<MeshDrawInstance>> MaterialGroupScratch = [];
        private readonly List<SimpleMaterial> MaterialGroupOrder = [];
        private TransformQ[] InstanceStaging = [];
        private nuint InstanceCapacity;
        private readonly List<DrawGroup> DrawGroups = [];

        private struct DrawGroup {
            public Mesh Mesh;
            public SimpleMaterial Material;
            public int InstanceBase;
            public int InstanceCount;
            public DrawGroup(Mesh mesh, SimpleMaterial material, int instanceBase, int instanceCount) {
                Mesh = mesh;
                Material = material;
                InstanceBase = instanceBase;
                InstanceCount = instanceCount;
            }
        }

        private List<MeshDrawInstance> RentList() {
            if(GroupListPoolIndex >= GroupListPool.Count) GroupListPool.Add([]);
            var list = GroupListPool[GroupListPoolIndex++];
            list.Clear();
            return list;
        }

        private void RenderPass(List<MeshDrawInstance>? meshes, float alphaCutoff, ref RenderStats stats) {
            if(meshes == null) return;

            var gl = window.OpenGL;

            //Orphan the instance buffer once per pass so in-flight draws keep the previous storage
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceBuffer);
            if(InstanceCapacity > 0) {
                unsafe { gl.BufferData(BufferTargetARB.ArrayBuffer, InstanceCapacity, null, BufferUsageARB.StreamDraw); }
            }

            //Group meshes by mesh
            GroupListPoolIndex = 0;
            MeshGroupScratch.Clear();
            MeshGroupOrder.Clear();
            foreach (var instance in meshes) {
                if (!MeshGroupScratch.TryGetValue(instance.Mesh, out var meshList)) {
                    meshList = RentList();
                    MeshGroupScratch[instance.Mesh] = meshList;
                    MeshGroupOrder.Add(instance.Mesh);
                }
                meshList.Add(instance);
            }
            //Stage all instance data of this pass contiguously, recording draw ranges
            DrawGroups.Clear();
            int cursor = 0;
            int instanceStride = Unsafe.SizeOf<TransformQ>();
            foreach (var mesh in MeshGroupOrder) {
                stats.ModelCount++;
                var instances = MeshGroupScratch[mesh];
                if (instances.Count == 0 || mesh.Vertices.Count == 0 || mesh.Indices.Count == 0) continue;

                //For each material
                MaterialGroupScratch.Clear();
                MaterialGroupOrder.Clear();
                foreach (var instance in instances) {
                    if (!MaterialGroupScratch.TryGetValue(instance.Material, out var materialList)) {
                        materialList = RentList();
                        MaterialGroupScratch[instance.Material] = materialList;
                        MaterialGroupOrder.Add(instance.Material);
                    }
                    materialList.Add(instance);
                }
                foreach (var material in MaterialGroupOrder) {
                    stats.MaterialCount++;
                    var materialInstances = MaterialGroupScratch[material];

                    if (materialInstances.Count == 0) continue;
                    int count = materialInstances.Count;
                    if(InstanceStaging.Length < cursor + count) InstanceStaging = new TransformQ[GrowCapacity(InstanceStaging.Length, cursor + count)];
                    for(int i = 0; i < count; i++) InstanceStaging[cursor + i] = materialInstances[i].Transform;
                    DrawGroups.Add(new DrawGroup(mesh, material, cursor, count));
                    cursor += count;
                }
            }

            //Upload all instance data of this pass in one call
            if(cursor > 0) {
                gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceBuffer);
                nuint neededBytes = (nuint)(cursor * instanceStride);
                if(neededBytes > InstanceCapacity) {
                    InstanceCapacity = (nuint)GrowCapacity((int)InstanceCapacity, (int)neededBytes);
                    unsafe { gl.BufferData(BufferTargetARB.ArrayBuffer, InstanceCapacity, null, BufferUsageARB.StreamDraw); }
                }
                gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, InstanceStaging.AsSpan(0, cursor));
            }

            foreach (var group in DrawGroups) {
                var mesh = group.Mesh;
                var material = group.Material;

                //Bind the mesh
                var meshGPU = GetCachedMesh(mesh);
                gl.BindVertexArray(meshGPU._vertexArray);
                CheckError("BindVertexArray");
                meshGPU.BindInstanceRange((nuint)(group.InstanceBase * instanceStride));

                //Bind uniforms
                ShaderUniformData sud = default;
                sud.AlphaCutoff = alphaCutoff;
                sud.AmbientColor = AmbientColor.Value;
                sud.WorldViewProjection = WorldViewProjection;
                sud.EmissiveIsMask = material.EmissiveIsMask;
                gl.BindBuffer(BufferTargetARB.UniformBuffer, _uniformBuffer);
                gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, [sud]);
                if (material.CullBack) {
                    gl.Enable(EnableCap.CullFace);
                } else {
                    gl.Disable(EnableCap.CullFace);
                }

                //Bind textures. For now, black and car.
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2D, GetCachedTexture(material.Texture).GetHandle());
                gl.ActiveTexture(TextureUnit.Texture1);
                gl.BindTexture(TextureTarget.Texture2D, GetCachedTexture(material.Emissive).GetHandle());

                stats.DrawCount++;
                unsafe {
                    gl.DrawElementsInstanced(PrimitiveType.Triangles, (uint)(mesh.Indices.Count), DrawElementsType.UnsignedShort, null, (uint)group.InstanceCount);
                }
                CheckError("DrawElementsInstanced");
            }
        }

        [Conditional("DEBUG")]
        private void CheckError(string component) {
            var error = window.OpenGL.GetError();
            if (error != GLEnum.NoError)
                throw new Exception($"OpenGL error in {component}: {error}");
        }

        /// <summary>
        /// Releases system resources held by this RenderManager
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Dispose() {
            GC.SuppressFinalize(this);

            //Destroy shaders
            var gl = window.OpenGL;
            gl.DetachShader(_meshProgram, _vertexShader);
            gl.DetachShader(_meshProgram, _fragmentShader);
            gl.DeleteShader(_vertexShader);
            gl.DeleteShader(_fragmentShader);
            gl.DeleteProgram(_meshProgram);
            _meshProgram = 0;
            _vertexShader = 0;
            _fragmentShader = 0;

            //Destroy caches
            foreach (var row in MeshCache) 
                row.Value.Dispose();
            MeshCache.Clear();
            window.OpenGL.DeleteBuffer(_instanceBuffer);
            _instanceBuffer = 0;
        }
    }
}
