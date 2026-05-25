using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {

    /// <summary>
    /// Shared interface for various road classes.
    /// <see cref="Lane"/>
    /// <see cref="LaneEnd"/>
    /// <see cref="RoadNode"/>
    /// <see cref="RoadNodeEnd"/>
    /// <see cref="RoadStrip"/>
    /// <see cref="LaneStrip"/>
    /// <see cref="LaneRange"/>
    /// </summary>
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
