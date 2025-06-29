using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Roads {
    public static class Util {
        public static NodeEnd Negate(this NodeEnd end) => (end == NodeEnd.Backward) ? NodeEnd.Forward : NodeEnd.Backward;
    }
}
