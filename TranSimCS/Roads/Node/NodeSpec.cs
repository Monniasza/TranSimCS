using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using TranSimCS.Geometry;

namespace TranSimCS.Roads.Node {
    public sealed class LaneNode(LaneSpec laneSpec, float centerPos, Guid? guid = null){
        public readonly Guid ID = guid ?? Guid.NewGuid();
        public readonly LaneSpec LaneSpec = laneSpec;
        public readonly float CenterPos = centerPos;

        public LaneNode WithBounds(Range<float> bounds) {
            var laneSpec = LaneSpec;
            var centerPos = (bounds.Min + bounds.Max) /2;
            laneSpec.Width = bounds.Max - bounds.Min;
            return new LaneNode(laneSpec, centerPos);
        }
        public Range<float> Bounds {
            get => new(CenterPos - LaneSpec.Width / 2, CenterPos + LaneSpec.Width / 2);
        }
    }
    public sealed class NodeSpec {
        public readonly IList<LaneNode> Lanes;
        public readonly IDictionary<Guid, LaneNode> LaneXRef;
        public readonly Range<float> Range;
        public NodeSpec(IEnumerable<LaneNode> data) {
            Range = data.Select(x => x.Bounds).AggregateOrDefault(new(0, 0), (x, y) => x.Union(y));
            Lanes = data.Order().ToImmutableList();
            LaneXRef = data.Select(x => new KeyValuePair<Guid, LaneNode>(x.ID, x)).ToImmutableDictionary();
        }
    }
}
