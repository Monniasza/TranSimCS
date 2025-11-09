using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public interface IRoadElement: IGuid {
        public int ZDiscriminant();
        public int XDiscriminant();
        public LaneStrip? GetLaneStrip();
        public RoadStrip? GetRoadStrip();
        public RoadNode? GetRoadNode();
        public Lane? GetLane();
        public LaneEnd? GetLaneEnd();
        public RoadNodeEnd? GetNodeEnd();
    }
}
