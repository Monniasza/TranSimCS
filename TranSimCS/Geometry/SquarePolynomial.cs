using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Geometry {
    public record struct SquarePolynomial(float A, float B, float C) {
        public float this[float t] => (A*t + B) * t + C;
        
        public static SquarePolynomial operator +(SquarePolynomial left, SquarePolynomial right) => new(left.A+right.A, left.B+right.B, left.C+right.C);
        public static SquarePolynomial operator +(SquarePolynomial left, float y) => new(left.A + y, left.B + y, left.C + y);
        public static SquarePolynomial operator -(SquarePolynomial left, SquarePolynomial right) => new(left.A - right.A, left.B - right.B, left.C - right.C);
        public static SquarePolynomial operator -(SquarePolynomial left, float y) => new(left.A - y, left.B - y, left.C - y);
        public static SquarePolynomial operator *(SquarePolynomial left, float y) => new(left.A * y, left.B * y, left.C * y);
        public static SquarePolynomial operator /(SquarePolynomial left, float y) => new(left.A / y, left.B / y, left.C / y);
        public static SquarePolynomial operator -(SquarePolynomial x) => new(-x.A, -x.B, -x.C);
        public static SquarePolynomial operator +(float y, SquarePolynomial p) => p + y;
        public static SquarePolynomial operator -(float y, SquarePolynomial p) => -p + y;
        public static SquarePolynomial operator *(float y, SquarePolynomial p) => p * y;
        public static implicit operator SquarePolynomial(float y) => new(0, 0, y);

        public float Derivative(float t) => (2 * A * t) + B;
        public Vector2 Solve() => Solve(A, B, C);
        public static Vector2 Solve(float A, float B, float C) {
            var D = B * B - 4 * A * C;
            var rD = MathF.Sqrt(D);
            var BpmRD = new Vector2(B + rD, B - rD);
            return BpmRD / (-2 * A);
        }
    }
}
