using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Geometry {
    public static class BoundingBoxMethods {
        public static float SurfaceArea(this BoundingBox box) {
            var dimensions = box.Max - box.Min;
            return 2 * ((dimensions.X * (dimensions.Y + dimensions.Z)) + (dimensions.Y * dimensions.Z));
        }
        public static float Extent(this BoundingBox box) {
            return Vector3.Distance(box.Min, box.Max);
        }
    }
}
