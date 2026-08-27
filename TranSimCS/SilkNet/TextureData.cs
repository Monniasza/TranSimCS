using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;

namespace TranSimCS.SilkNet {
    /// <summary>
    /// Just raw texture date. No renderer specific information.
    /// </summary>
    public sealed class TextureData {
        public uint Width { get; }
        public uint Height { get; }
        public TextureFormat Format { get; }
        public ReadOnlyMemory<byte> Data { get; }

        public TextureData(
            uint width,
            uint height,
            TextureFormat format,
            ReadOnlyMemory<byte> data) {
            Width = width;
            Height = height;
            Format = format;
            Data = data;
        }

        public TextureData(MagickImage image) {
            Format = image.GetPreferredFormat();
            Width = image.Width;
            Height = image.Height;
            var rawData = image.DumpPixelData(Format);
            Data = new ReadOnlyMemory<byte>(rawData);
        }
    }
}
