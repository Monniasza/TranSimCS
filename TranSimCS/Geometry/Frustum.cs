using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Geometry {
    public struct Frustum : IEquatable<Frustum> {
        public Plane Left;
        public Plane Right;
        public Plane Top;
        public Plane Bottom;
        public Plane Near;
        public Plane Far;

        public Frustum(Plane left, Plane right, Plane top, Plane bottom, Plane near, Plane far) {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
            Near = near;
            Far = far;
        }
        public override bool Equals(object? obj) {
            return obj is Frustum frustum && Equals(frustum);
        }

        public bool Equals(Frustum other) {
            return Left.Equals(other.Left) &&
                   Right.Equals(other.Right) &&
                   Top.Equals(other.Top) &&
                   Bottom.Equals(other.Bottom) &&
                   Near.Equals(other.Near) &&
                   Far.Equals(other.Far);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Left, Right, Top, Bottom, Near, Far);
        }

        public static bool operator ==(Frustum left, Frustum right) {
            return left.Equals(right);
        }

        public static bool operator !=(Frustum left, Frustum right) {
            return !(left == right);
        }

        /// <summary>
        /// Creates a bounding frustum from a matrix
        /// </summary>
        public static Frustum FromViewProjection(Matrix4x4 matrix) {
            var r1 = new Vector4(matrix.M11, matrix.M12, matrix.M13, matrix.M14);
            var r2 = new Vector4(matrix.M21, matrix.M22, matrix.M23, matrix.M24);
            var r3 = new Vector4(matrix.M31, matrix.M32, matrix.M33, matrix.M34);
            var r4 = new Vector4(matrix.M41, matrix.M42, matrix.M43, matrix.M44);
            Frustum result = default;
            result.Left = CreatePlane(r4 + r1);
            result.Right = CreatePlane(r4 - r1);
            result.Bottom = CreatePlane(r4 + r2);
            result.Top = CreatePlane(r4 - r2);
            result.Near = CreatePlane(r4 + r3);
            result.Far = CreatePlane(r4 - r3);
            return result;
        }

        public static Plane CreatePlane(Vector4 v) => Plane.Normalize(new Plane(v));

        public float? Intersect(Ray3 ray, float minT = 0, float maxT = float.PositiveInfinity) {
            float UpdateResult(float t, float sofar) {
                if (t >= minT && t <= maxT) return float.Max(t, sofar);
                return sofar;
            }

            var tFar = ray.Intersect(Far);
            var tNear = ray.Intersect(Near);
            var tLeft = ray.Intersect(Left);
            var tRight = ray.Intersect(Right);
            var tTop = ray.Intersect(Top);
            var tBottom = ray.Intersect(Bottom);

            if (tFar < minT && tNear < minT && tLeft < minT && tRight < minT && tTop < minT && tBottom < minT)
                return minT;

            float result = float.PositiveInfinity;
            result = UpdateResult(tFar, result);
            result = UpdateResult(tNear, result);
            result = UpdateResult(tLeft, result);
            result = UpdateResult(tRight, result);
            result = UpdateResult(tTop, result);
            result = UpdateResult(tBottom, result);
            if (result < minT || result > maxT) return null;
            return result;
        }

        public void Planes(ref Span<Plane> dest) {
            dest[0] = Far;
            dest[1] = Near;
            dest[2] = Left;
            dest[3] = Right;
            dest[4] = Top;
            dest[5] = Bottom;
        }
        public void Planes(Plane[] dest) {
            dest[0] = Far;
            dest[1] = Near;
            dest[2] = Left;
            dest[3] = Right;
            dest[4] = Top;
            dest[5] = Bottom;
        }

        public Intersection Intersect(AABB boundingBox) {
            Span<Plane> planes = stackalloc Plane[6];
            Planes(ref planes);
            bool intersecting = false;
            for (int i = 0; i < 6; i++) {
                var intersect = boundingBox.Intersect(planes[i]);
                if (intersect == Intersection.Contained) return Intersection.Disjoint;
                if (intersect == Intersection.Intersecting) intersecting = true;
            }
            return intersecting ? Intersection.Intersecting : Intersection.Contained;
        }
        public bool Intersects(AABB boundingBox) => Intersect(boundingBox) != Intersection.Disjoint;
    }
}
