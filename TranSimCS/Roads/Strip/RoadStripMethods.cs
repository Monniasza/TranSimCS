using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads.Node;
using TranSimCS.Spline;

namespace TranSimCS.Roads.Strip {
    public static class RoadStripMethods {
        public static bool IsSingleEnded(this RoadStrip strip) => strip.StartNode == strip.EndNode;
        public static IndexStrip GenerateDegenerateIndexStrips(this RoadNodeEnd node) {
            var tangent = node.CalcReferenceFrame().Z;
            var bounds = node.Range();
            var scale = 2.0f / 3;
            var scaledTangent = (bounds.Max - bounds.Min) * tangent * scale;
            IndexPoint left = new(bounds.Min, scaledTangent);
            IndexPoint right = new(bounds.Max, scaledTangent);
            return new(left, right, left, right);
        }
    }
}
