using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;

namespace TranSimCS.Roads.StripGenerator {
    public sealed class AnisotropicStripSplineGenerator : StripSplineGenerator {
        private AnisotropicStripSplineGenerator() : base("anisotropic") { }
        public static AnisotropicStripSplineGenerator Instance = new();

        public override IndexSpline GenerateSplines(Transform3 startReference, Transform3 endReference, Vector2 startEnd)
            => SplineAlgorithms.GenerateSegmentSplinedUsingAlg(startReference, endReference, startEnd, SplineAlgorithms.AnisotropicSpline);
    }
}
