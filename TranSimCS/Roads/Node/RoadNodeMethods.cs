using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Roads.Node {
    public static class RoadNodeMethods {
        public static HalfLane[] GetAllHalfLanes(this RoadNode node) {
            var hlanes = new HalfLane[node.Lanes.Count * 2];
            int i = 0;
            foreach (var lane in node.Lanes) { 
                hlanes[i++] = lane.FrontHalf;
                hlanes[i++] = lane.RearHalf;
            }
            return hlanes;
        }
    }
}
