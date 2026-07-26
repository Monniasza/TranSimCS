using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using TranSimCS.Spline;

namespace TranSimCS.Roads.Strip {
    public class RoadStripCache {
        public RoadStrip RoadStrip { get; private set; }

        private RoadBounds? _bounds;
        public RoadBounds Bounds => _bounds ??= GenerateBounds();
        private OrthodistantBasis? _splineFrame;
        public OrthodistantBasis OrthodistantBasis => _splineFrame ??= GenerateOrthodistantBasis();
        private IndexSpline? _indexStrip;
        public IndexSpline IndexStrip => _indexStrip ??= GenerateIndexStrip();

        public RoadStripCache(RoadStrip roadStrip) {
            RoadStrip = roadStrip;
        }

        private IndexSpline GenerateIndexStrip() {
            if (RoadStrip.IsSingleEnded()) {
                //The RoadStrip has only one end
                return RoadStrip.StartNode.GenerateDegenerateIndexStrips();
            } else {
                //The RoadStrip joins node-ends
                return RoadStrip.SplineGenerator.GenerateSplines(RoadStrip);
            }
        }
        private OrthodistantBasis GenerateOrthodistantBasis() => IndexStrip.ToOrthodistantBasis(RoadStrip.StartNode, RoadStrip.EndNode);
        private RoadBounds GenerateBounds() {
            var bounds = new RoadBounds();
            foreach (var lane in RoadStrip.Lanes) {
                var startLane = lane.StartLane;
                var endLane = lane.EndLane;
                if (startLane.HalfNode == RoadStrip.EndNode & endLane.HalfNode == RoadStrip.StartNode && startLane.HalfNode != endLane.HalfNode) {
                    (startLane, endLane) = (endLane, startLane);
                }

                var startBounds = startLane.Lane.Bounds;
                var endBounds = endLane.Lane.Bounds;

                bounds = bounds
                    .Update(startBounds.Min, endBounds.Min)
                    .Update(startBounds.Max, endBounds.Max);
            }
            if (bounds.leftStart > bounds.rightStart || bounds.leftEnd > bounds.rightEnd) {
                bounds.leftStart = bounds.rightStart = bounds.leftEnd = bounds.rightEnd = 0;
            }
            return bounds;
        }
    }
}
