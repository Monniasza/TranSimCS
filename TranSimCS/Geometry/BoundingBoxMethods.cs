using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Geometry {
    public static class BoundingBoxMethods {
        public static float SurfaceArea(this AABB box) {
            var dimensions = box.Max - box.Min;
            return 2 * ((dimensions.X * (dimensions.Y + dimensions.Z)) + (dimensions.Y * dimensions.Z));
        }
        public static float Extent(this AABB box) {
            return Vector3.Distance(box.Min, box.Max);
        }
    }
}
