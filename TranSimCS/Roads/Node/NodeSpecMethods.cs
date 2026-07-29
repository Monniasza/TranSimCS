using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Roads.Node {
    public static class NodeSpecMethods {
        public static NodeSpec ToNodeSpec(this IEnumerable<LaneNode> lanes) => new NodeSpec(lanes);
        public static NodeSpec ToNodeSpec(this IEnumerable<Lane> lanes) => lanes.Select(x => x.LaneNode).ToNodeSpec();
        public static NodeSpec ToNodeSpec(this IEnumerable<HalfLane> lanes) => lanes.Select(x => x.LaneNode).ToNodeSpec();
    }
}
