using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using TranSimCS.Geometry;
using TranSimCS.Roads.Range;
using TranSimCS.Spline;

namespace TranSimCS.Roads.Strip {
    public class RoadStripCache {
        public RoadStrip RoadStrip { get; private set; }

        private LaneRange? _bounds;
        public LaneRange Bounds => _bounds ??= GenerateBounds();
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
        private LaneRange GenerateBounds() {
            Range<float> startRange = default;
            Range<float> endRange = default;
            foreach (var lane in RoadStrip.Lanes) {
                var startLane = lane.StartLane;
                var endLane = lane.EndLane;
                if (lane.IsReverse()) DataUtil.Swap(ref startLane, ref endLane);
                startRange = startRange.Union(startLane.Bounds);
                endRange = endRange.Union(endLane.Bounds);
            }
            return new(RoadStrip, startRange, endRange);
        }
    }
}
