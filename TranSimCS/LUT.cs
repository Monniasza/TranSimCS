using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TranSimCS {
    public struct LUTKey: IComparable<LUTKey>, IEquatable<LUTKey> {
        public float X;
        public Vector4 Y;
        public LUTKey(float x, Vector4 y) {
            X = x;
            Y = y;
        }

        public int CompareTo(LUTKey other) => X.CompareTo(other.X);

        public override bool Equals(object? obj) => obj is LUTKey key && Equals(key);

        public bool Equals(LUTKey key) => X == key.X && Y.Equals(key.Y);

        public override int GetHashCode() {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(LUTKey left, LUTKey right) {
            return left.Equals(right);
        }

        public static bool operator !=(LUTKey left, LUTKey right) {
            return !(left == right);
        }

        public static bool operator <(LUTKey left, LUTKey right) {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(LUTKey left, LUTKey right) {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(LUTKey left, LUTKey right) {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(LUTKey left, LUTKey right) {
            return left.CompareTo(right) >= 0;
        }
    }
    public sealed class LUT: IEnumerable<LUTKey> {
        private LUTKey[] Data;
        public LUT(IEnumerable<LUTKey> y) {
            ArgumentNullException.ThrowIfNull(y, nameof(y));
            if (y.Count() == 0) throw new ArgumentException("The list must not be empty");
            Data = y.OrderBy(x => x.X).ToArray();
        }
        public Vector4 this[float x] { get {
                if (Data.Length == 1) return Data[0].Y;
                if (Data.Length == 2) {
                    var x1 = Data[0].X;
                    var x2 = Data[1].X;
                    var unlerp = (x - x1) / (x2 - x1);
                    return Vector4.Lerp(Data[0].Y, Data[1].Y, unlerp);
                }
                var dummy = new LUTKey() { X = x };
                var binarySearch = Array.BinarySearch(Data, dummy);
                if(binarySearch < 0) {
                    //Interpolated find
                    var next = ~binarySearch;
                    if (next == 0) next = 1;
                    if (next == Data.Length) next = Data.Length - 1;
                    var prev = next - 1;
                    var a = Data[prev];
                    var b = Data[next];
                    var unlerp = (x - a.X) / (b.X - a.X);
                    return Vector4.Lerp(a.Y, b.Y, unlerp);
                } else {
                    //Exact find
                    return Data[binarySearch].Y;
                }
        }}

        public IEnumerator<LUTKey> GetEnumerator() => ((IEnumerable<LUTKey>)Data).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
