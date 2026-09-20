using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;

namespace TranSimCS.Mode.RoadBuilder {
    public class RoadBuilderLibrary {
        public TSWorld World {get; private set;}
        public Dictionary<string, LaneSpec> LaneSpecs { get; private set; } = [];
        public Dictionary<string, RoadFinish> RoadFinishes { get; private set; } = [];
        public Dictionary<string, NodeSpec> NodeSpecs { get; private set; } = [];
    }
}
