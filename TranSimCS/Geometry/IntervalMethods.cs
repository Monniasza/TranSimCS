using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TranSimCS.Geometry {
    public static class IntervalMethods{
        public static Interval<T> MinMax<T>(this T a, T b) where T : IComparable<T> {
            var cmp = a.CompareTo(b);
            if(cmp > 0) DataUtil.Swap(ref a, ref b);
            return new (a, b);
        }
        public static Interval<T> Union<T>(this Interval<T> a, Interval<T> b) where T : IComparable<T> {
            if (a.IsDegenerate) return b;
            if (b.IsDegenerate) return a;
            var min = MinMax(a.Min, b.Min).Min;
            var max = MinMax(a.Max, b.Max).Max;
            return new(min, max);
        }
        public static Interval<T> IntervalUnion<T>(this IEnumerable<Interval<T>> ranges) where T : IComparable<T> => ranges.Aggregate(Union);
        public static Interval<T> Intersection<T>(this Interval<T> a, Interval<T> b) where T : IComparable<T> {
            var min = MinMax(a.Min, b.Min).Max;
            var max = MinMax(a.Max, b.Max).Min;
            var cmp = min.CompareTo(max);
            if (cmp < 0) return new(min, min);
            return new(min, max);
        }
        public static Interval<T> IntervalIntersection<T>(this IEnumerable<Interval<T>> ranges) where T : IComparable<T> => ranges.Aggregate(Intersection);
        public static void Deconstruct<T>(this Interval<T> range, out T min, out T max) where T : IComparable<T> {
            min = range.Min;
            max = range.Max;
        }

        public static T Middle<T>(this Interval<T> a) where T: INumber<T> {
            return (a.Min + a.Max)/(T.One + T.One);
        }
        public static T Width<T>(this Interval<T> a) where T : INumber<T> {
            return a.Max - a.Min;
        }
    }
}
