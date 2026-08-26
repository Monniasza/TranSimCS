using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;

namespace TranSimCS.Roads.StripGenerator {
    public sealed class ClassicStripSplineGenerator: StripSplineGenerator {
        private ClassicStripSplineGenerator() : base("isotropic") { }
        public static ClassicStripSplineGenerator Instance = new();
        public override IndexSpline GenerateSplines(Transform3 startReference, Transform3 endReference, Vector2 startEnd)
            => SplineAlgorithms.GenerateSegmentSplinedUsingAlg(startReference, endReference, startEnd, SplineAlgorithms.IsotropicSpline);
    }
}
