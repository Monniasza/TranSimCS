using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;

namespace TranSimCS.Geometry {
    public static class RangeMethods{
        public static Range<T> MinMax<T>(this T a, T b) where T : IComparable<T> {
            var cmp = a.CompareTo(b);
            if(cmp < 0) DataUtil.Swap(ref a, ref b);
            return new (a, b);
        }
        public static Range<T> Union<T>(this Range<T> a, Range<T> b) where T : IComparable<T> {
            if (a.IsDegenerate) return b;
            if (b.IsDegenerate) return a;
            var min = MinMax(a.Min, b.Min).Min;
            var max = MinMax(a.Max, b.Max).Max;
            return new(min, max);
        }
        public static Range<T> Intersection<T>(this Range<T> a, Range<T> b) where T : IComparable<T> {
            var min = MinMax(a.Min, b.Min).Max;
            var max = MinMax(a.Max, b.Max).Min;
            var cmp = min.CompareTo(max);
            if (cmp < 0) return new(min, min);
            return new(min, max);
        }
    }
}
