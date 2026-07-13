using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;

namespace TranSimCS.Roads.Node {
    public static class LaneDefinitionMethods {
        public static Range<float> Bounds(this LaneDefinition definition) {
            var halfwidth = definition.LaneSpec.Width / 2;
            return new(definition.CenterPosition - halfwidth, definition.CenterPosition + halfwidth);
        }

        public static LaneDefinition Mirror(this LaneDefinition definition) => new(-definition.CenterPosition, definition.LaneSpec);
    }
}
