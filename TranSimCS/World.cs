using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads;

namespace TranSimCS
{
    public class World
    {
        public List<LaneConnection> RoadSegments { get; } = new List<LaneConnection>();
        public List<RoadNode> RoadNodes { get; } = new List<RoadNode>();
    }
}
