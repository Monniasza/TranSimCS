using System;

namespace TranSimCS.Roads {
    public enum NodeEnd {
        Forward, Backward
    }
    public static class NodeEndMethods {
        public static NodeEnd Negate(this NodeEnd end) => (end == NodeEnd.Backward) ? NodeEnd.Forward : NodeEnd.Backward;
        public static T GetConditional<T>(this NodeEnd end, T backward, T forward) {
            if (end == NodeEnd.Backward) return backward;
            if (end == NodeEnd.Forward) return forward;
            throw new ArgumentException($"Invalid node end: {end}");
        }
    }
}
