using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clipper2Lib;
using MadWorldNL.EarCut.Logic;

namespace TranSimCS.Geometry {
    public static class Triangulate2D {
        public static List<int> TriangulatePolygon(IEnumerable<PointD> points) {
            return EarCut.Tessellate<double>(points.SelectMany(x => new double[] { x.x, x.y }).ToArray(), null, 2);
        }
    }
}
