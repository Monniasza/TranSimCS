using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Geometry {
    public struct Ray3 : IEquatable<Ray3> {
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }

        public Ray3(Vector3 origin, Vector3 direction) {
            Origin = origin;
            Direction = direction;
        }

        public Vector3 GetPoint(float distance)
            => Origin + Direction * distance;

        public override bool Equals(object? obj) {
            return obj is Ray3 ray && Equals(ray);
        }

        public bool Equals(Ray3 other) {
            return Origin.Equals(other.Origin) &&
                   Direction.Equals(other.Direction);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Origin, Direction);
        }

        public static bool operator ==(Ray3 left, Ray3 right) {
            return left.Equals(right);
        }

        public static bool operator !=(Ray3 left, Ray3 right) {
            return !(left == right);
        }

        public float? Intersects(AABB box, float min = 0, float max = float.PositiveInfinity, float slack = 0) {
            float tmin = min;
            float tmax = max;
            var boxSlack = new Vector3(slack);
            var boxMin = box.Min - boxSlack;
            var boxMax = box.Max + boxSlack;

            //X intersection
            var invX = 1 / Direction.X;
            var invY = 1 / Direction.Y;
            var invZ = 1 / Direction.Z;

            if(MathF.Abs(Direction.X) >= 0.0000001) {
                float x1 = (boxMin.X - Origin.X) * invX;
                float x2 = (boxMax.X - Origin.X) * invX;
                tmin = MathF.Max(tmin, MathF.Min(x1, x2));
                tmax = MathF.Min(tmax, MathF.Max(x1, x2));
            }
            if (MathF.Abs(Direction.Y) >= 0.0000001) {
                float y1 = (boxMin.Y - Origin.Y) * invY;
                float y2 = (boxMax.Y - Origin.Y) * invY;
                tmin = MathF.Max(tmin, MathF.Min(y1, y2));
                tmax = MathF.Min(tmax, MathF.Max(y1, y2));
            }
            if (MathF.Abs(Direction.Z) >= 0.0000001) {
                float z1 = (boxMin.Z - Origin.Z) * invZ;
                float z2 = (boxMax.Z - Origin.Z) * invZ;
                tmin = MathF.Max(tmin, MathF.Min(z1, z2));
                tmax = MathF.Min(tmax, MathF.Max(z1, z2));
            }
            if (tmin > tmax || tmax < min) return null;

            if (tmin < min) return tmax;
            return tmin;
        }

        public float? Intersects(Plane plane, float min = 0, float max = float.PositiveInfinity) {
            var result = -(Vector3.Dot(Origin, plane.Normal) + plane.D) / (Vector3.Dot(Direction, plane.Normal));
            if(result < min || result > max) return null;
            return result;
        }
        public float Intersect(Plane plane) => -(Vector3.Dot(Origin, plane.Normal) + plane.D) / (Vector3.Dot(Direction, plane.Normal));
    }
}
