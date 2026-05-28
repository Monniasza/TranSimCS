using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Roads.StripGenerator {
    public sealed class ClassicStripSplineGenerator: StripSplineGenerator {
        private ClassicStripSplineGenerator() : base("isotropic") { }
        public static ClassicStripSplineGenerator Instance = new();

        public override SplineFrame GenerateSplines(RoadStrip road) {
            var start = road.StartNode.CalcReferenceFrame();
            var end = road.EndNode.CalcReferenceFrame();
            var zeroSpline = GeometryUtils.GenerateJoinSpline(start.O, end.O, start.Z, end.Z);
            var xSpline = GeometryUtils.GenerateJoinSpline(start.O + start.X, end.O - end.X, start.Z, end.Z);
            var ySpline = GeometryUtils.GenerateJoinSpline(start.O + start.Y, end.O + end.Y, start.Z, end.Z);
            return new SplineFrame(zeroSpline, xSpline - zeroSpline, ySpline - zeroSpline, new Spline.Bezier3());
        }
    }
}
