using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGL;

namespace TranSimCS.SilkNet {
    public sealed class GLBuffer: IDisposable {
        private GL _gl;
        public BufferTargetARB Target { get; private set; }
        private uint _vboHandle;

        public GLBuffer(GL gl) {
            _gl = gl;
            _vboHandle = _gl.GenBuffer();
        }

        public void Dispose() {
            GetContext();
            _gl.DeleteBuffer(_vboHandle);
            _vboHandle = 0;
            _gl = null;
        }

        public void Bind() {
            GetContext();
            _gl.BindBuffer(Target, _vboHandle);
        }

        /// <summary>
        /// Gets the assigned Open<see cref="GL"/> context and checks if this <see cref="VAO"/> has been disposed
        /// </summary>
        /// <returns>the assigned Open<see cref="GL"/> context, or else throws</returns>
        /// <exception cref="ObjectDisposedException">if this <see cref="VAO"/> has been disposed</exception>
        public GL GetContext() => _gl ?? throw new ObjectDisposedException(nameof(_gl));
        /// <summary>
        /// Gets the low level VAO handle
        /// </summary>
        /// <returns></returns>
        public uint GetVAOHandle() {
            GetContext();
            return _vboHandle;
        }
    }
}
