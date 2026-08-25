using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LanguageExt.SomeHelp;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.SilkNet {
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex : IEquatable<Vertex> {
        public Vector3 Position;
        public Color Color;
        public Vector2 TexCoord;
        public ushort Material;
        public ushort Emissive;

        public Vertex(Vector3 position, Color color, Vector2 texCoord, ushort material = 0, ushort emissive = 0) {
            Position = position;
            Color = color;
            TexCoord = texCoord;
            Material = material;
        }
        public Vertex(Vector3 position, Color color, Vector2 texCoord) {
            Position = position;
            Color = color;
            TexCoord = texCoord;
            Material = 0;
        }
        public Vertex(Vector3 position, Vector2 texCoord) {
            Position = position;
            Color = Color.White;
            TexCoord = texCoord;
            Material = 0;
        }

        public Vertex(VertexPositionColorTexture vpct) {
            Position = vpct.Position.ToNumerics();
            Color = Color.FromArgb((int)vpct.Color.PackedValue);
        }

        public override bool Equals(object? obj) {
            return obj is Vertex vertex && Equals(vertex);
        }

        public bool Equals(Vertex other) {
            return Position.Equals(other.Position) &&
                   Color.Equals(other.Color) &&
                   TexCoord.Equals(other.TexCoord) &&
                   Material == other.Material;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Position, Color, TexCoord, Material);
        }

        public static bool operator ==(Vertex left, Vertex right) {
            return left.Equals(right);
        }

        public static bool operator !=(Vertex left, Vertex right) {
            return !(left == right);
        }
    }
}
