using Microsoft.Xna.Framework;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;

namespace TranSimCS.Roads.Range {
    /// <summary>
    /// Represents a pair of endpoints in half-node convention for a surface strip
    /// </summary>
    public struct RoadSplineRange {
        public Vector3 startLeft;
        public Vector3 endLeft;
        public Vector3 startRight;
        public Vector3 endRight;

        public RoadSplineRange(Vector3 startLeft, Vector3 endLeft, Vector3 startRight, Vector3 endRight) {
            this.startLeft = startLeft;
            this.endLeft = endLeft;
            this.startRight = startRight;
            this.endRight = endRight;
        }
    }
    public static class RoadSplineRangeMethods {
        public static (Vector3[] Left, Vector3[] Right) GenerateRoadSplineRange(this RoadSplineRange range, RoadStripData strip) => (
            RoadStrip.GenerateSplineHalfNode(strip, range.startLeft, range.endLeft),
            RoadStrip.GenerateSplineHalfNode(strip, range.startRight, range.endRight)
        );
    }
}