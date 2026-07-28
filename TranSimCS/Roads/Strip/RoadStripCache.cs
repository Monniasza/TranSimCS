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

        private RoadStripData? _roadStripData;
        public RoadStripData RoadStripData => _roadStripData ??= GenerateRoadStripData();

        private LaneRange? _bounds;
        public LaneRange Bounds => _bounds ??= GenerateBounds();
        public OrthodistantBasis OrthodistantBasis => RoadStripData.OrthodistantBasis;
        private IndexSpline? _indexStrip;
        public IndexSpline IndexStrip => RoadStripData.IndexSpline;

        public RoadStripCache(RoadStrip roadStrip) {
            RoadStrip = roadStrip;
        }

        private RoadStripData GenerateRoadStripData() {
            RoadDataBuilder builder = new();
            builder.RoadFinish = RoadStrip.Finish;
            builder.EndLanes = RoadStrip.EndNode.NodeSpec;
            builder.StartLanes = RoadStrip.StartNode.NodeSpec;
            builder.EndPos = RoadStrip.EndNode.PositionProp.Value;
            builder.StartPos = RoadStrip.StartNode.PositionProp.Value;
            builder.IndexSpline = GenerateIndexStrip();
            foreach(var strip in RoadStrip.Lanes) {
                var start = strip.StartLane.LaneNode;
                var end = strip.EndLane.LaneNode;
                var spec = strip.Spec;
                builder.AddConnection(start, end, spec);
            }
            return builder.Create();
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
