using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;

namespace TranSimCS.Roads.StripGenerator {
    public sealed class AnisotropicStripSplineGenerator : StripSplineGenerator {
        private AnisotropicStripSplineGenerator() : base("anisotropic") { }
        public static AnisotropicStripSplineGenerator Instance = new();

        /*public override SplineFrame GenerateSplines(RoadStrip road) {
            var start = road.StartNode.CalcReferenceFrame();
            var end = road.EndNode.CalcReferenceFrame();
            var zeroSpline = SplineAlgorithms.AnisotropicSpline(start.O, start.Z, end.O, end.Z);
            var xSpline = SplineAlgorithms.AnisotropicSpline(start.O + start.X, start.Z, end.O - end.X, end.Z);
            var ySpline = SplineAlgorithms.AnisotropicSpline(start.O + start.Y, start.Z, end.O + end.Y, end.Z);
            return new SplineFrame(zeroSpline, xSpline - zeroSpline, ySpline - zeroSpline, new Spline.Bezier3());
        }*/
        public override IndexStrip GenerateSplines(RoadStrip road)
            => SplineAlgorithms.GenerateSegmentSplinedUsingAlg(road, SplineAlgorithms.AnisotropicSpline);
    }
}
