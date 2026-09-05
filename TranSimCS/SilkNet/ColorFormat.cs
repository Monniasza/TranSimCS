using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGL;

namespace TranSimCS.SilkNet {
    public enum TextureFormat {
        R16,
        RG16,
        RGB16,
        RGBA16,
        RGB8,
        RGBA8
    }

    public static class TextureFormatMehods {
        public static (PixelType Type, PixelFormat Format, InternalFormat Internal) GetGLFormats(this TextureFormat tf) {
            return tf switch {
                TextureFormat.R16 => (PixelType.UnsignedByte, PixelFormat.Red, InternalFormat.R16),
                TextureFormat.RG16 => (PixelType.UnsignedByte, PixelFormat.Red, InternalFormat.RG16),
                TextureFormat.RGB16 => (PixelType.UnsignedByte, PixelFormat.Red, InternalFormat.Rgb16),
                TextureFormat.RGBA16 => (PixelType.UnsignedByte, PixelFormat.Red, InternalFormat.Rgba16),
                TextureFormat.RGB8 => (PixelType.UnsignedByte, PixelFormat.Rgb, InternalFormat.Rgb8),
                TextureFormat.RGBA8 => (PixelType.UnsignedByte, PixelFormat.Rgba, InternalFormat.Rgba8),
                _ => throw new ArgumentException("Invalid TextureFormat")
            };
        }
    }
}
