using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace TranSimCS.SilkNet {
    public class TextureGL : IDisposable {
        public PixelType PixelType { get; private set; }
        public PixelFormat PixelFormat { get; private set; }
        public InternalFormat InternalFormat { get; private set; }
        public Image Image { get; private set; }
        internal uint _handle;
        internal GL _gl;

        public TextureGL(Image image, GL gl) {
            Image = image;
            var pixelInfo = image.PixelType;
            (PixelType, PixelFormat, InternalFormat) = image.GetGLTypes();
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


        public void Upload(Image image) {
            Type imageType = image.GetType();

            if (!imageType.IsGenericType ||
                imageType.GetGenericTypeDefinition() != typeof(Image<>)) {
                throw new NotSupportedException(
                    $"Unsupported ImageSharp image type: {imageType}");
            }

            Type pixelType = imageType.GetGenericArguments()[0];

            MethodInfo generalMethod = typeof(TextureGL)
                .GetMethod(nameof(UploadGeneric), BindingFlags.Public | BindingFlags.Instance);
            Debug.Assert(generalMethod != null, "No UploadGeneric method found");
            MethodInfo method = generalMethod.MakeGenericMethod(pixelType);

            method.Invoke(this, [image]);
        }
        public unsafe void UploadGeneric<TPixel>(Image<TPixel> image) where TPixel: unmanaged, IPixel<TPixel> {
            GetContext();
            int byteCount = image.Width * image.Height * Unsafe.SizeOf<TPixel>();
            var data = new byte[byteCount];
            image.CopyPixelDataTo(data);
            fixed (byte* ptr = data) {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat,
                    (uint)image.Width,
                    (uint)image.Height,
                    0,
                    PixelFormat,
                    PixelType,
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
