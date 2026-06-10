using System;
using System.Numerics;
using MonoGame.Extended;
using TranSimCS.Geometry.SplineFrames;

namespace TranSimCS.Roads.Node {
    public enum NodeEnd {
        Forward, Backward
    }
    public static class NodeEndMethods {
        public static NodeEnd Negate(this NodeEnd end) => end == NodeEnd.Backward ? NodeEnd.Forward : NodeEnd.Backward;
        public static T GetConditional<T>(this NodeEnd end, T backward, T forward) {
            if (end == NodeEnd.Backward) return backward;
            if (end == NodeEnd.Forward) return forward;
            throw new ArgumentException($"Invalid node end: {end}");
        }
        public static int Discriminant(this NodeEnd end) {
            if (end == NodeEnd.Forward) return 1;
            if (end == NodeEnd.Backward) return -1;
            return 0;
        }

        public static Range<T> ConvertConventions<T>(this NodeEnd end, Range<T> range) where T : IUnaryNegationOperators<T, T>, IComparable<T> {
            if(end == NodeEnd.Backward) return new(-range.Max, -range.Min);
            return range;
        }
        public static T ConvertConventions<T>(this NodeEnd end, T value) where T : IUnaryNegationOperators<T, T>
            => (end == NodeEnd.Backward) ? -value : value;
    }
}
