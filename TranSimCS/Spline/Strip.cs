using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Spline {
    public struct Strip(Bezier3 left, Bezier3 right): IPatch {
        public Bezier3 left = left;
        public Bezier3 right = right;

        public Vector3 Get(float x, float y) {
            var lpos = left[y];
            var rpos = right[y];
            return Vector3.Lerp(lpos, rpos, x);
        }

        public IPatch SubRange(float x0, float y0, float x1, float y1) {
            var leftSubstrip = left.SubRange(y0, y1);
            var rightSubstrip = right.SubRange(y0, y1);
            var leftSubstrip2 = Bezier3.Lerp(leftSubstrip, rightSubstrip, x0);
            var rightSubstrip2 = Bezier3.Lerp(leftSubstrip, rightSubstrip, x1);
            return new Strip(leftSubstrip2, rightSubstrip2);
        }

        
    }
}
