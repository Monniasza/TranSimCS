using System;
using TranSimCS.Roads;

namespace TranSimCS.Tools {
    public struct LaneMapping {
        public int StartIndex;
        public int EndIndex;
        public LaneSpec LaneSpec;
        public Guid? PassedGuid;
        public LaneMapping(int startIndex, int endIndex, LaneSpec laneSpec, Guid? passedGuid = null) {
            StartIndex = startIndex;
            EndIndex = endIndex;
            LaneSpec = laneSpec;
            PassedGuid = passedGuid;
        }
    }
}
