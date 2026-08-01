using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.StripData;

namespace TranSimCS.Tools.RoadConstruction {
    public static class ConnectionValidation {
        public static bool IsConnectionValid(RoadStripData state) {
            //Check: reject invalid positions
            if(!state.OrthodistantBasis.IsFinite()) return false;

            return true;
        }
    }
}
