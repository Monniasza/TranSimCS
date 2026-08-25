using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Arch.LowLevel.Jagged;
using DotNet.Collections.Generic;
using LanguageExt;
using LanguageExt.Pipes;
using LanguageExt.UnitsOfMeasure;
using Microsoft.Xna.Framework;
using Silk.NET.OpenGL;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.Property;
using TranSimCS.Render;
using TranSimCS.Setting;
using TranSimCS.Terrain;
using TranSimCS.Tools;
using static TranSimCS.Model.MeshUnroll;

namespace TranSimCS.SilkNet {
    public partial class RenderManager: IDisposable {
        public readonly Property<Camera> CameraProp;
        public readonly Property<Vector4> AmbientColor;
        public readonly SilkNetTest window;
        public Matrix WorldViewProjection { get; private set; }
        public Matrix World { get; private set; }
        public Matrix View { get; private set; }
        public Matrix Projection { get; private set; }
        private readonly Dictionary<Mesh, MeshGPU> MeshCache = [];
        
        internal uint _instanceBuffer;
        internal uint _vertexShader;
        internal uint _fragmentShader;
        internal uint _meshProgram;
        internal uint _uniformBuffer;

        public static readonly string MeshVertSource;
        public static readonly string FragSource;
        static RenderManager() {
            MeshVertSource = TerrainDataBlobs.ReadEmbeddedResource("TranSimCS.Include.mesh.vert");
            FragSource = TerrainDataBlobs.ReadEmbeddedResource("TranSimCS.Include.mesh.frag");
        }


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
                meshGPU.Dispose();
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

        public void Render(MultiMesh source) {
            //CONSTANTS
            var gl = window.OpenGL;

            gl.Enable(EnableCap.DepthTest);
            gl.UseProgram(_meshProgram);
            CheckError("UseProgram");
            

            //CATEGORIZATION & COUNTING
            var stats = new RenderStats();
            var allMeshes = MeshTraversal.Traverse(source).ToArray();

            var groups = new List<MeshDrawInstance>?[(int)MaterialBlendMode.Count];
            for(int i = 0; i < allMeshes.Length; i++) {
                var instance = allMeshes[i];
                var list = groups[(int)instance.Material.BlendMode] ??= [];
                list.Add(instance);
            }

            var groupByRenderType = allMeshes.QuickGroup(x => x.Material.BlendMode);

            //Bind per-pass attributes
            gl.Disable(EnableCap.Blend);
            RenderPass(groups[(int)MaterialBlendMode.Opaque], 0, ref stats);
            RenderPass(groups[(int)MaterialBlendMode.Cutout], 0.5f, ref stats);

            gl.Enable(EnableCap.Blend);
            RenderPass(groups[(int)MaterialBlendMode.Transparent], 0, ref stats);

            Stats = stats;
        }

        private void RenderPass(IEnumerable<MeshDrawInstance>? meshes, float alphaCutoff, ref RenderStats stats) {
            if(meshes == null) return;

            var gl = window.OpenGL;

            //Group meshes by mesh
            var groupedMeshes = meshes.QuickGroup(x => x.Mesh);
            stats.ModelCount += groupedMeshes.Count;
            foreach (var meshGroup in groupedMeshes) {
                var mesh = meshGroup.Key;
                var instances = meshGroup.Value;
                if (instances.Count == 0 || mesh.Vertices.Count == 0 || mesh.Indices.Count == 0) continue;

                //Bind the mesh
                var meshGPU = GetCachedMesh(mesh);
                gl.BindVertexArray(meshGPU._vertexArray);
                CheckError("BindVertexArray");

                //For each material
                var groupedByMaterial = instances.QuickGroup(x => x.Material);
                stats.MaterialCount += groupedByMaterial.Count;
                foreach (var materialGroup in groupedByMaterial) {
                    var material = materialGroup.Key;
                    var materialInstances = materialGroup.Value;

                    if (materialInstances.Count == 0) continue;
                    var positionValues = materialInstances.Select(x => x.Transform).ToArray();

                    //Bind uniforms
                    ShaderUniformData sud = default;
                    sud.AlphaCutoff = alphaCutoff;
                    sud.AmbientColor = AmbientColor.Value.ToNumerics();
                    sud.WorldViewProjection = WorldViewProjection.ToNumerics();
                    sud.EmissiveIsMask = material.EmissiveIsMask;
                    gl.BindBuffer(BufferTargetARB.UniformBuffer, _uniformBuffer);
                    gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, [sud]);

                    //Bind textures. For now, black and car.
                    gl.ActiveTexture(TextureUnit.Texture0);
                    gl.BindTexture(TextureTarget.Texture2D, window.CarTex.GetHandle());
                    gl.ActiveTexture(TextureUnit.Texture1);
                    gl.BindTexture(TextureTarget.Texture2D, window.CarEmissive.GetHandle());

                    //Upload instance data
                    gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceBuffer);
                    gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(positionValues.Length * 28), positionValues, BufferUsageARB.DynamicDraw);

                    stats.DrawCount++;
                    unsafe {
                        gl.DrawElementsInstanced(PrimitiveType.Triangles, (uint)(mesh.Indices.Count), DrawElementsType.UnsignedShort, null, (uint)(instances.Count));

                        //gl.DrawElements(PrimitiveType.Triangles, (uint)(mesh.Indices.Count), DrawElementsType.UnsignedShort, null);
                    }
                    CheckError("DrawElementsInstanced");
                    //throw new Exception($"Got to the drawcall. Index count: {mesh.Indices.Count}, Vertex count: {mesh.Vertices.Count}, Instance count: {instances.Count}");
                }
            }
        }

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
