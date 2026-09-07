using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Geometry {
    public struct AABB : IEquatable<AABB> {
        public Vector3 Min;
        public Vector3 Max;

        public AABB(Vector3 v) {
            Min = v;
            Max = v;
        }

        public AABB(Vector3 min, Vector3 max) {
            Min = min;
            Max = max;
        }

        public override bool Equals(object? obj) {
            return obj is AABB aABB && Equals(aABB);
        }

        public bool Equals(AABB other) {
            return Min.Equals(other.Min) &&
                   Max.Equals(other.Max);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Min, Max);
        }

        public static bool operator ==(AABB left, AABB right) {
            return left.Equals(right);
        }

        public static bool operator !=(AABB left, AABB right) {
            return !(left == right);
        }

        public void Vertices(ref Span<Vector3> points) {
            points[0] = Min;
            points[1] = new(Min.X, Min.Y, Max.Z);
            points[2] = new(Min.X, Max.Y, Min.Z);
            points[3] = new(Min.X, Max.Y, Max.Z);
            points[4] = new(Max.X, Min.Y, Min.Z);
            points[5] = new(Max.X, Min.Y, Max.Z);
            points[6] = new(Max.X, Max.Y, Min.Z);
            points[7] = Max;
        }
        public void Vertices(Vector3[] points) {
            points[0] = Min;
            points[1] = new(Min.X, Min.Y, Max.Z);
            points[2] = new(Min.X, Max.Y, Min.Z);
            points[3] = new(Min.X, Max.Y, Max.Z);
            points[4] = new(Max.X, Min.Y, Min.Z);
            points[5] = new(Max.X, Min.Y, Max.Z);
            points[6] = new(Max.X, Max.Y, Min.Z);
            points[7] = Max;
        }

        /// <summary>
        /// Intersects this axis-aligned bounding box with a plane.
        /// If all vertices are either on the plane or on its negative side, the AABB is considered <see cref="Intersection.Contained"/>
        /// If all vertices are either on the plane or on its positive side, the AABB is considered <see cref="Intersection.Disjoint"/>
        /// Otherwise it is considered <see cref="Intersection.Intersecting"/>
        /// </summary>
        public Intersection Intersect(Plane plane) {
            Span<Vector3> points = stackalloc Vector3[8]; 
            Vertices(ref points);
            Span<float> distances = stackalloc float[8];
            for(int i = 0; i < 8; i++) distances[i] = plane.PrenormSignedDistance(points[i]);
            byte state = 0;
            for (int i = 0; i < 8; i++) {
                var dist = distances[i];
                if (dist < -0.001) state |= 1;
                else if (dist > 0.001) state |= 2;
                else state |= 4;
            }
            Debug.Assert(state != 0, "No points are inside, across or outside the plane");
            if (state == 1 || state == 5) return Intersection.Contained;
            if (state == 2 || state == 6) return Intersection.Disjoint;
            return Intersection.Intersecting;
        }

        public bool Intersects(Plane plane) => Intersect(plane) != Intersection.Disjoint;
        public bool Intersects(AABB aabb){
            bool intersectsX = Max.X >= aabb.Min.X || Min.X <= aabb.Max.X;
            bool intersectsY = Max.Y >= aabb.Min.Y || Min.Y <= aabb.Max.Y;
            bool intersectsZ = Max.Z >= aabb.Min.Z || Min.Z <= aabb.Max.Z;
            return intersectsX && intersectsY && intersectsZ;
        }

        public static AABB CreateMerged(AABB a, AABB b) {
            if(a.Min == a.Max) return b;
            if(b.Min == b.Max) return a;
            return new(Vector3.Min(a.Min, b.Min), Vector3.Max(a.Max, b.Max));
        }
    }
}
