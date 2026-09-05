using TranSimCS.Geometry;

namespace TranSimCS.Roads.Node {
    public static class LaneDefinitionMethods {
        public static Interval<float> Bounds(this LaneDefinition definition) {
            var halfwidth = definition.LaneSpec.Width / 2;
            return new(definition.CenterPosition - halfwidth, definition.CenterPosition + halfwidth);
        }

        public static LaneDefinition Mirror(this LaneDefinition definition) => new(-definition.CenterPosition, definition.LaneSpec);
    }
}
