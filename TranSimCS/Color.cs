using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;

namespace TranSimCS {
    [StructLayout(LayoutKind.Explicit)]
    public struct Color : IEquatable<Color> {
        [FieldOffset(0)]
        public byte R;
        [FieldOffset(1)]
        public byte G;
        [FieldOffset(2)]
        public byte B;
        [FieldOffset(3)]
        public byte A;
        [FieldOffset(0)]
        public uint PackedValue;

        public Color(byte r, byte g, byte b, byte a = 255) {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color(Vector4 vector) {
            R = (byte)(vector.X.Clamp(0, 1) * 255);
            G = (byte)(vector.Y.Clamp(0, 1) * 255);
            B = (byte)(vector.Z.Clamp(0, 1) * 255);
            A = (byte)(vector.W.Clamp(0, 1) * 255);
        }
        public Color(Microsoft.Xna.Framework.Color c) => PackedValue = c.PackedValue;

        public Vector4 ToVector4() => new(R / 255f, G / 255f, B / 255f, A / 255f);
        public Color MulAlpha(float alpha) => new Color(ToVector4() * alpha);

        public Microsoft.Xna.Framework.Color ToMonoGame() => new Microsoft.Xna.Framework.Color(PackedValue);


        public static Color White => new(255, 255, 255);
        public static Color Black => new(0, 0, 0);
        public static Color Transparent => new(0, 0, 0, 0);

        public bool Equals(Color other) => PackedValue == other.PackedValue;
        public override int GetHashCode() => PackedValue.GetHashCode();
    }
}
