using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using Microsoft.Xna.Framework.Graphics;
using Silk.NET.OpenGL;
using TranSimCS.Geometry;
using TranSimCS.Model;

namespace TranSimCS.SilkNet {
    //An internal mesh cache for RenderManager
    internal class MeshGPU: IDisposable{
        public RenderManager rm;
        public int UploadedVersion = -1;
        public uint _vertexBuffer;
        public uint _indexBuffer;
        public uint _vertexArray;
        public MeshGPU(RenderManager rm) {
            //Create the mesh buffers
            var gl = rm.window.OpenGL;
            this.rm = rm;
            _vertexArray = gl.GenVertexArray();
            _indexBuffer = gl.GenBuffer();
            _vertexBuffer = gl.GenBuffer();

            var testStride = Unsafe.SizeOf<Vertex>();
            Debug.Assert(testStride == 28, $"Mismatched vertex size: {testStride} != 28");
            Debug.Assert(Marshal.OffsetOf<Vertex>(nameof(Vertex.Position)) == 0);
            Debug.Assert(Marshal.OffsetOf<Vertex>(nameof(Vertex.Color)) == 12);
            Debug.Assert(Marshal.OffsetOf<Vertex>(nameof(Vertex.TexCoord)) == 16);
            Debug.Assert(Marshal.OffsetOf<Vertex>(nameof(Vertex.Material)) == 24);
            Debug.Assert(Marshal.OffsetOf<Vertex>(nameof(Vertex.Emissive)) == 26);

            gl.BindVertexArray(_vertexArray);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
            
            var stride = (uint)Unsafe.SizeOf<VertexPositionColorTexture>();
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            gl.EnableVertexArrayAttrib(_vertexArray, 0);
            gl.VertexAttribPointer(1, 4, VertexAttribPointerType.UnsignedByte, true, stride, 12);
            gl.EnableVertexArrayAttrib(_vertexArray, 1);
            gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 16);
            gl.EnableVertexArrayAttrib(_vertexArray, 2);

            var instanceStride = (uint)Unsafe.SizeOf<TransformQ>();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, rm._instanceBuffer);
            gl.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, instanceStride, 0);
            gl.EnableVertexArrayAttrib(_vertexArray, 3);
            gl.VertexAttribDivisor(3, 1);
            gl.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, instanceStride, 12);
            gl.EnableVertexArrayAttrib(_vertexArray, 4);
            gl.VertexAttribDivisor(4, 1);

            gl.BindVertexArray(0);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        }
        public void Dispose() {
            var gl = rm.window.OpenGL;
            gl.DeleteBuffer(_vertexBuffer);
            gl.DeleteBuffer(_indexBuffer);
            gl.DeleteVertexArray(_vertexArray);
            _vertexBuffer = 0;
            _indexBuffer = 0;
            _vertexArray = 0;
            UploadedVersion = int.MaxValue;
        }
        public void CheckAndRefresh(Mesh mesh) {
            if(mesh.GeometryVersion != UploadedVersion) {
                //Regenerate the mesh data
                var gl = rm.window.OpenGL;
                var usage = BufferUsageARB.DynamicDraw;
                    
                gl.BindVertexArray(_vertexArray);
                gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
                gl.BufferData(BufferTargetARB.ArrayBuffer, mesh.Vertices.AsSpan(), usage);
                gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, mesh.Indices.AsSpan(), usage);
                UploadedVersion = mesh.GeometryVersion;
            }
        }
    }
}
