using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Roads {
    public class SplineTransformer {
        public static Vector3 Transform(Vector3 vector, SplineFrame splineFrame) {
            // Transform the vector using the spline frame's coordinate system
            // This transforms a relative position vector into world coordinates using the spline frame
            var result = splineFrame.CenterSpline.a +
                        vector.X * splineFrame.XPlusSpline.a +
                        vector.Y * splineFrame.YPlusSpline.a +
                        vector.Z * splineFrame.ZPlusSpline.a;
            return result;
        }
    }
}
