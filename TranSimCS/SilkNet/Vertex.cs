using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TranSimCS.SilkNet {
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex : IEquatable<Vertex>{
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
            Emissive = emissive;
        }
        public Vertex(Vector3 position, Color color, Vector2 texCoord) {
            Position = position;
            Color = color;
            TexCoord = texCoord;
            Material = 0;
            Emissive = 0;
        }
        public Vertex(Vector3 position, Vector2 texCoord) {
            Position = position;
            Color = Colors.White;
            TexCoord = texCoord;
            Material = 0;
            Emissive = 0;
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
