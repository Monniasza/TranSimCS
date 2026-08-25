using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TranSimCS.SilkNet {
    public static class PixelFormatInfoMethods {
        public static (PixelType Type, PixelFormat Format, InternalFormat Internal) GetGLTypes(this Image image) => ToOpenGL(image.GetType().GetGenericArguments()[0]);

        public static (PixelType Type, PixelFormat Format, InternalFormat Internal) ToOpenGL(Type pixelType) {
            return pixelType switch {
                _ when pixelType == typeof(Rgba32)
                    => (PixelType.UnsignedByte, PixelFormat.Rgba, InternalFormat.Rgba),

                _ when pixelType == typeof(Bgra32)
                    => (PixelType.UnsignedByte, PixelFormat.Bgra, InternalFormat.Rgba),

                _ when pixelType == typeof(Rgb24)
                    => (PixelType.UnsignedByte, PixelFormat.Rgb, InternalFormat.Rgb),

                _ when pixelType == typeof(Bgr24)
                    => (PixelType.UnsignedByte, PixelFormat.Bgr, InternalFormat.Rgb),

                _ when pixelType == typeof(L8)
                    => (PixelType.UnsignedByte, PixelFormat.Red, InternalFormat.Red),

                _ when pixelType == typeof(L16)
                    => (PixelType.UnsignedShort, PixelFormat.Red, InternalFormat.Red),

                // ...

                _ => throw new NotSupportedException(
                    $"No OpenGL representation for {pixelType}.")
            };
        }
        public static (PixelType Type, PixelFormat Format, InternalFormat Internal) ToOpenGL<TPixel>()
        where TPixel : unmanaged, IPixel<TPixel> => ToOpenGL(typeof(TPixel));
    }
}
