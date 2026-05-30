using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;

namespace TranSimCS.Roads.StripGenerator {
    public sealed class ClassicStripSplineGenerator: StripSplineGenerator {
        private ClassicStripSplineGenerator() : base("isotropic") { }
        public static ClassicStripSplineGenerator Instance = new();
        public override IndexStrip GenerateSplines(RoadStrip road)
            => SplineAlgorithms.GenerateSegmentSplinedUsingAlg(road, SplineAlgorithms.IsotropicSpline);
    }
}
