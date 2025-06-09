using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS
{
    public class World
    {
        public List<RoadSegment> RoadSegments { get; } = new List<RoadSegment>();
        public List<RoadNode> RoadNodes { get; } = new List<RoadNode>();
    }
}
