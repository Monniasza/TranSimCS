using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Geometry {
    public struct Vector3i(int x, int y, int z): IComparable<Vector3i>, IEquatable<Vector3i>{
        public int x = x;
        public int y = y;
        public int z = z;

        public static Vector3i One => new Vector3i(1, 1, 1);

        public static int Radix => 10;

        public static Vector3i Zero => new(0, 0, 0);

        public static Vector3i AdditiveIdentity => Zero;

        public static Vector3i MultiplicativeIdentity => One;

        public static Vector3i Abs(Vector3i value) {
            return new Vector3i(Math.Abs(value.x), Math.Abs(value.y), Math.Abs(value.z));
        }
        public static Vector3i Max(Vector3i x, Vector3i y) {
            return new(Math.Max(x.x, y.x), Math.Max(x.y, y.y), Math.Max(x.z, y.z));
        }

        public static Vector3i Min(Vector3i x, Vector3i y) {
            return new(Math.Min(x.x, y.x), Math.Min(x.y, y.y), Math.Min(x.z, y.z));
        }

        public int CompareTo(object? obj) {
            if(obj is Vector3i other) return CompareTo(other); return 0;
        }

        public int CompareTo(Vector3i other) {
            var cmpX = x.CompareTo(other.x);
            if(cmpX != 0)  return cmpX;
            var cmpY = y.CompareTo(other.y);
            if(cmpY != 0) return cmpY;
            return z.CompareTo(other.z);
        }

        public bool Equals(Vector3i other) {
            return x == other.x && y == other.y && z == other.z;
        }

        public override string ToString() {
            return $"({x}, {y}, {z})";
        }

        public static Vector3i operator +(Vector3i left, Vector3i right) => new(left.x + right.x, left.y + right.y, left.z + right.z);

        public static Vector3i operator -(Vector3i value) => new(-value.x, -value.y, -value.z);

        public static Vector3i operator -(Vector3i left, Vector3i right) => new(left.x - right.x, left.y - right.y, left.z - right.z);

        public static Vector3i operator *(Vector3i left, Vector3i right) => new(left.x * right.x, left.y * right.y, left.z * right.z);

        public static Vector3i operator *(Vector3i left, int right) => new(left.x * right, left.y * right, left.z * right);

        public static Vector3 operator *(Vector3i left, float right) => new(left.x * right, left.y * right, left.z * right);

        public static Vector3i operator /(Vector3i left, Vector3i right) => new(left.x / right.x, left.y / right.y, left.z / right.z);

        public static Vector3i operator /(Vector3i left, int right) => new(left.x / right, left.y / right, left.z / right);

        public static Vector3 operator /(Vector3i left, float right) => new(left.x / right, left.y / right, left.z / right);

        public static Vector3i operator %(Vector3i left, Vector3i right) => new(left.x / right.x, left.y / right.y, left.z / right.z);

        public static int Dot(Vector3i left, Vector3i right) => (left * right).Sum();

        public int Sum() => x + y + z;

        public static bool operator ==(Vector3i left, Vector3i right) => left.Equals(right);

        public static bool operator !=(Vector3i left, Vector3i right) => !left.Equals(right);

        public static bool operator <(Vector3i left, Vector3i right) => left.CompareTo(right) < 0;

        public static bool operator >(Vector3i left, Vector3i right) => left.CompareTo(right) > 0;

        public static bool operator <=(Vector3i left, Vector3i right) => left.CompareTo(right) <= 0;

        public static bool operator >=(Vector3i left, Vector3i right) => left.CompareTo(right) >= 0;

        public static implicit operator Vector3(Vector3i v) => new Vector3(v.x, v.y, v.z);
        public static explicit operator Vector3i(Vector3 v) => new Vector3i((int)v.X, (int)v.Y, (int)v.Z);

        public override bool Equals(object? obj) {
            if(obj is Vector3i v) return Equals(v); return false;
        }

        public override int GetHashCode() {
            return HashCode.Combine(x, y, z);
        }
    }

    public static class SpanMethods {
        public static string SpanToString(this ReadOnlySpan<char> span) {
            return new string(span.ToArray());
        }
    }
}
