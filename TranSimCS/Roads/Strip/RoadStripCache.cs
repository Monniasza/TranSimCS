using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using TranSimCS.Geometry.SplineFrames;
using TranSimCS.Spline;

namespace TranSimCS.Roads.Strip {
    public class RoadStripCache {
        public RoadStrip RoadStrip { get; private set; }

        private RoadBounds? _bounds;
        public RoadBounds Bounds => _bounds ??= GenerateBounds();
        private SplineFrame? _splineFrame;
        public SplineFrame SplineFrame => _splineFrame ??= GenerateSplineFrame();
        private IndexStrip? _indexStrip;
        public IndexStrip IndexStrip => _indexStrip ??= GenerateIndexStrip();

        public RoadStripCache(RoadStrip roadStrip) {
            RoadStrip = roadStrip;
        }

        private IndexStrip GenerateIndexStrip() {
            if (RoadStrip.IsSingleEnded()) {
                //The RoadStrip has only one end
                return RoadStrip.StartNode.GenerateDegenerateIndexStrips();
            } else {
                //The RoadStrip joins node-ends
                return RoadStrip.SplineGenerator.GenerateSplines(RoadStrip);
            }
        }
        private SplineFrame GenerateSplineFrame() => IndexStrip.ToSplineFrame(RoadStrip.StartNode, RoadStrip.EndNode);
        private RoadBounds GenerateBounds() {
            var bounds = new RoadBounds();
            foreach (var lane in RoadStrip.Lanes) {
                var startLane = lane.StartLane;
                var endLane = lane.EndLane;
                if (startLane.RoadNodeEnd == RoadStrip.EndNode & endLane.RoadNodeEnd == RoadStrip.StartNode && startLane.RoadNodeEnd != endLane.RoadNodeEnd) {
                    (startLane, endLane) = (endLane, startLane);
                }

                var startBounds = startLane.lane.Bounds;
                var endBounds = endLane.lane.Bounds;

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
