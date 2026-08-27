using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace TranSimCS.SilkNet {
    public class TextureGPU : IDisposable {
        public TextureData Image { get; private set; }
        internal uint _handle;
        internal GL _gl;

        public TextureGPU(TextureData image, GL gl) {
            Image = image;
            _gl = gl ?? throw new ArgumentNullException(nameof(gl));
            _handle = gl.GenTexture();
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, _handle);
            gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.Repeat);
            gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.Repeat);
            gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest);
            gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);
            Upload(image);
        }


        public unsafe void Upload(TextureData image) {
            GetContext();

            //Convert image data
            var data = image.Data;
            var (pixelType, pixelFormat, internalFormat) = image.Format.GetGLFormats();
            fixed (byte* ptr = data.Span) {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    internalFormat,
                    (uint)image.Width,
                    (uint)image.Height,
                    0,
                    pixelFormat,
                    pixelType,
                    ptr);
            }
        }

        public GL GetContext() => _gl ?? throw new ObjectDisposedException(nameof(_gl));
        public uint GetHandle() {
            GetContext();
            return _handle;
        }

        public void Dispose() {
            GetContext();
            _gl.DeleteTexture(_handle);
            _handle = 0;
            _gl = null;
        }
    }
}
