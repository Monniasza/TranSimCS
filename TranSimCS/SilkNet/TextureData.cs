using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;
using Silk.NET.Core;

namespace TranSimCS.SilkNet {
    /// <summary>
    /// Just raw texture data. No renderer specific information.
    /// </summary>
    public sealed class TextureData : IEquatable<TextureData?> {
        private static uint idCounter = 1;
        private readonly uint ID;
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
            ID = idCounter++;
        }

        public TextureData(MagickImage image) {
            Format = image.GetPreferredFormat();
            Width = image.Width;
            Height = image.Height;
            var rawData = image.DumpPixelData(Format);
            Data = new ReadOnlyMemory<byte>(rawData);
            ID = idCounter++;
        }

        public override bool Equals(object? obj) {
            return Equals(obj as TextureData);
        }

        public bool Equals(TextureData? other) {
            return other is not null &&
                   ID == other.ID;
        }

        public override int GetHashCode() {
            return HashCode.Combine(ID);
        }

        public static bool operator ==(TextureData? left, TextureData? right) {
            return (left is null) ? right is null : right is not null && left.ID == right.ID;
        }

        public static bool operator !=(TextureData? left, TextureData? right) {
            return !(left == right);
        }

        public RawImage ToRawImage() {
            uint size = Width * Height;
            byte[] rawPixels = new byte[size * 4];
            var (bytesPerChannel, channels) = Format.GetToRGBAFormats();
            var stride = bytesPerChannel * channels;
            var shiftRight = (bytesPerChannel - 1);
            var data = Data.Span;

            byte o = 0;
            byte j = 255;

            for (int i = 0; i < size; i++) {
                var offset = shiftRight + i * stride;
                byte R = data[offset];
                offset += bytesPerChannel;
                byte G = (channels > 1) ? data[offset] : o;
                offset += bytesPerChannel;
                byte B = (channels > 2) ? data[offset] : o;
                offset += bytesPerChannel;
                byte A = (channels > 3) ? data[offset] : j;
                rawPixels[0 + i * 4] = R;
                rawPixels[1 + i * 4] = G;
                rawPixels[2 + i * 4] = B;
                rawPixels[3 + i * 4] = A;
            }

            return new((int)Width, (int)Height, new(rawPixels));
        }
    }
}
