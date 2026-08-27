using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;

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
    }
}
