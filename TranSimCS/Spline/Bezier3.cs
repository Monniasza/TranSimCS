using System;
using System.Runtime.Intrinsics.X86;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Spline
{

    public struct LineSegment: ISpline<Vector3> {
        public Vector3 a;
        public Vector3 b;
        public Vector3 this[float t] => Vector3.Lerp(a, b, t);
        public LineSegment(Vector3 a, Vector3 b) {
            this.a = a;
            this.b = b;
        }
        public LineSegment SubRange(float from, float to) {
            return new LineSegment(this[from], this[to]);
        }
        public LineSegment Inverse() => new(b, a);
    }

    public struct Bezier3: ISpline<Vector3> {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        public Vector3 d;

        public Bezier3(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }
        public Bezier3(Vector3 a) {
            this.a = a;
            b = a;
            c = a;
            d = a;
        }

        public Vector3 this[float t] {
            get {
                float u = 1 - t;
                return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
            }
        }

        public static Vector3 Interpolate(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int t) {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            return uu * u * a + 3 * uu * t * b + 3 * u * tt * c + t * tt * d;
        }
        public static Vector4 BasisFunctions(int t) {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            return new(u * uu, t * uu, u * tt, t * tt);
        }

        public static Bezier3 operator +(Bezier3 first, Bezier3 other) {
            return new Bezier3 {
                a = first.a + other.a,
                b = first.b + other.b,
                c = first.c + other.c,
                d = first.d + other.d
            };
        }

        public static Bezier3 operator +(Bezier3 first, Vector3 other) {
            return new Bezier3 {
                a = first.a + other,
                b = first.b + other,
                c = first.c + other,
                d = first.d + other
            };
        }

        public static Bezier3 operator -(Bezier3 first, Bezier3 other) {
            return new Bezier3 {
                a = first.a - other.a,
                b = first.b - other.b,
                c = first.c - other.c,
                d = first.d - other.d
            };
        }

        public static Bezier3 operator -(Bezier3 first, Vector3 other) {
            return new Bezier3 {
                a = first.a - other,
                b = first.b - other,
                c = first.c - other,
                d = first.d - other
            };
        }
        public static Bezier3 operator *(Bezier3 bezier, float scalar) {
            return new Bezier3 {
                a = bezier.a * scalar,
                b = bezier.b * scalar,
                c = bezier.c * scalar,
                d = bezier.d * scalar
            };
        }
        public static Bezier3 operator /(Bezier3 bezier, float scalar) {
            if (scalar == 0) throw new DivideByZeroException("Cannot divide by zero.");
            return new Bezier3 {
                a = bezier.a / scalar,
                b = bezier.b / scalar,
                c = bezier.c / scalar,
                d = bezier.d / scalar
            };
        }

        public Bezier3 Inverse() => new(d, c, b, a);
        public Bezier3 SubRange(float from, float to) => LenientSubSection(this, from, to);
        public static Bezier3 Lerp(Bezier3 from, Bezier3 to, float t) {
            return new(
                Vector3.Lerp(from.a, to.a, t),
                Vector3.Lerp(from.b, to.b, t),
                Vector3.Lerp(from.c, to.c, t),
                Vector3.Lerp(from.d, to.d, t)
            );
        }

        public static Bezier3 LenientSubSection(Bezier3 bezier, float startT, float endT) {
            if (startT > endT) return SubSection(bezier, endT, startT).Inverse();
            if (startT == endT) return new Bezier3(bezier[startT]);
            return SubSection(bezier, startT, endT);

        }
        public static Bezier3 SubSection(Bezier3 bezier, float startT, float endT) {
            if (startT < 0 || endT > 1 || startT >= endT) throw new ArgumentOutOfRangeException("startT and endT must be in the range [0, 1] and startT < endT.");
            Split(bezier, startT, out var beginningSection, out var endSection);
            Split(endSection, (endT - startT) / (1 - startT), out var finalStart, out var finalEnd);
            return finalStart;
        }
        public static void TriSection(Bezier3 bezier, float startT, float endT, out Bezier3 startSection, out Bezier3 midSection, out Bezier3 endSection) {
            if (startT < 0 || endT > 1 || startT >= endT) throw new ArgumentOutOfRangeException("startT and endT must be in the range [0, 1] and startT < endT.");
            if(startT == 0 && endT == 1) {
                startSection = new Bezier3(bezier.a);
                midSection = bezier;
                endSection = new Bezier3(bezier.d);
                return;
            }else if(startT == 0) {
                startSection = new Bezier3(bezier.a);
                Split(bezier, endT, out midSection, out endSection);
                return;
            } else if(endT == 1) {
                Split(bezier, startT, out startSection, out midSection);
                endSection = new Bezier3(bezier.d);
                return;
            }

            Split(bezier, startT, out startSection, out var finalStart);
            Split(finalStart, (endT - startT) / (1 - startT), out midSection, out endSection);
        }

        public static void Split(Bezier3 source, float t, out Bezier3 start, out Bezier3 end) {
            var ab = Vector3.Lerp(source.a, source.b, t);
            var bc = Vector3.Lerp(source.b, source.c, t);
            var cd = Vector3.Lerp(source.c, source.d, t);
            var abc = Vector3.Lerp(ab, bc, t);
            var bcd = Vector3.Lerp(bc, cd, t);
            var abcd = Vector3.Lerp(abc, bcd, t);

            start.a = source.a;
            start.b = ab;
            start.c = abc;
            start.d = abcd;
            end.a = abcd;
            end.b = bcd;
            end.c = cd;
            end.d = source.d;
        }

        public static void SplitInHalf(Bezier3 source, out Bezier3 start, out Bezier3 end) {
            var ab = (source.a + source.b) / 2;
            var bc = (source.b + source.c) / 2;
            var cd = (source.c + source.d) / 2;
            var abc = (ab + bc) / 2;
            var bcd = (bc + cd) / 2;
            var abcd = (abc + bcd) / 2;

            start.a = source.a;
            start.b = ab;
            start.c = abc;
            start.d = abcd;
            end.a = abcd;
            end.b = bcd;
            end.c = cd;
            end.d = source.d;
        }

        /// <summary>
        /// Finds the t value on the spline that is closest to the given position.
        /// </summary>
        /// <param name="spline">source spline</param>
        /// <param name="pos">target point to find the nearest T to</param>
        /// <param name="accuracy">number of loop iterations</param>
        /// <param name="depth">number of iterations to find the closest point</param>
        /// <param name="lowerLimit">lower limit of the t value</param>
        /// <param name="upperLimit">upper limit of the t value</param>
        /// <param name="pointsPerCycle">number of points to generate per cycle</param>
        /// <returns></returns>
        public static float FindT(Bezier3 spline, Vector3 pos, int pointsPerCycle = 20, int depth = 5, float lowerLimit = 0, float upperLimit = 1) {
            if(depth <= 0) {
                return (lowerLimit + upperLimit) / 2f; // Return the average of the bounds if depth is exhausted
            }
            var points = GeometryUtils.GenerateSplinePoints(spline, pointsPerCycle, lowerLimit, upperLimit);
            int closestIndex = 0;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < points.Length; i++) {
                float distance = Vector3.Distance(points[i], pos);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }
            int minIndex = Math.Max(0, closestIndex - 2);
            int maxIndex = Math.Min(points.Length - 1, closestIndex + 2);
            float minT = minIndex / (float)(points.Length - 1);
            float maxT = maxIndex / (float)(points.Length - 1);
            minT = MathHelper.Lerp(lowerLimit, upperLimit, minT); // Scale minT to the original range
            maxT = MathHelper.Lerp(lowerLimit, upperLimit, maxT); // Scale maxT to the original range
            return FindT(spline, pos, pointsPerCycle, depth - 1, minT, maxT); // Recursively find the closest t value in the range
        }
    }
}
