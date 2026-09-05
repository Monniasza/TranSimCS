using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Geometry {
    public struct Interval<T> : IEquatable<Interval<T>> where T:IComparable<T> {
        public T Min { get; private set; }
        public T Max { get; private set; }

        public Interval(T min, T max) {
            var comparison = min.CompareTo(max);
            if (comparison > 0) throw new ArgumentException("min > max");
            Min = min;
            Max = max;
        }

        public bool IsDegenerate => EqualityComparer<T>.Default.Equals(Min, Max);
        public bool IsProper => !IsDegenerate;

        public override bool Equals(object? obj) {
            return obj is Interval<T> interval && Equals(interval);
        }

        public bool Equals(Interval<T> other) {
            return EqualityComparer<T>.Default.Equals(Min, other.Min) &&
                   EqualityComparer<T>.Default.Equals(Max, other.Max);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Min, Max);
        }

        public static bool operator ==(Interval<T> left, Interval<T> right) {
            return left.Equals(right);
        }

        public static bool operator !=(Interval<T> left, Interval<T> right) {
            return !(left == right);
        }
    }
}
