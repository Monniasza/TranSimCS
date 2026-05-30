using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;

namespace TranSimCS.Roads.StripGenerator {
    public sealed class AnisotropicStripSplineGenerator : StripSplineGenerator {
        private AnisotropicStripSplineGenerator() : base("anisotropic") { }
        public static AnisotropicStripSplineGenerator Instance = new();

        public override IndexStrip GenerateSplines(RoadStrip road)
            => SplineAlgorithms.GenerateSegmentSplinedUsingAlg(road, SplineAlgorithms.AnisotropicSpline);
    }
}
