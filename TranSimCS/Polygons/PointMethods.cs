using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clipper2Lib;

namespace TranSimCS.Polygons {
    public static class PointMethods {
        public static double Distance(this PointD a, PointD b) {
            var x = a.x - b.x;
            var y = a.y - b.y;
            return Math.Sqrt(x * x + y * y);
        }
    }
}
