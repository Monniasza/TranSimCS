using TranSimCS.Geometry;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;

namespace TranSimCS.Roads.StripGenerator {
    public sealed class AnisotropicStripSplineGenerator : StripSplineGenerator {
        private AnisotropicStripSplineGenerator() : base("anisotropic") { }
        public static AnisotropicStripSplineGenerator Instance = new();

        public override IndexSpline GenerateSplines(Transform3 startReference, Transform3 endReference, DualRange range)
            => SplineAlgorithms.GenerateSegmentSplinedUsingAlg(startReference, endReference, range, SplineAlgorithms.AnisotropicSpline);
    }
}
