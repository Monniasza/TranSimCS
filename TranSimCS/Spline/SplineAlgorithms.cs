using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Spline {
    public delegate Bezier3 SplineAlgorithm(Vector3 start, Vector3 startTangent, Vector3 end, Vector3 endTangent);

    public class SplineAlgorithms {
        public static Bezier3 AnisotropicSpline(Vector3 start, Vector3 startTangent, Vector3 end, Vector3 endTangent) {
            var flattenedStart = start.ToX0Z();
            var flattenedStartTangent = startTangent.ToX0Z();
            var flattenedEnd = end.ToX0Z();
            var flattenedEndTangent = endTangent.ToX0Z();

            flattenedStartTangent.Normalize();
            flattenedEndTangent.Normalize();
            flattenedStartTangent *= startTangent.Length();
            flattenedEndTangent *= endTangent.Length();

            var flattenedSpline = GeometryUtils.GenerateJoinSpline(flattenedStart, flattenedEnd, flattenedStartTangent, flattenedEndTangent);
            var anisotropicStartTangent = flattenedSpline.b - flattenedSpline.a;
            var anisotropicEndTangent = flattenedSpline.c - flattenedSpline.d;

            //Adjust the anisotropic tangents' Y to make them inline to source tangents
            // For each spline (anisoTangent, originalTangent): len(anisoTangent.xz) / newY = len(originalTangent.xz) / originalTangent.y
            // By rearranging, 1/newY = len(originalTangent.xz) / (originalTangent.y * len(anisoTangent.xz))
            // newY = originalTangent.y * len(anisoTangent.xz) / len(originalTangent.xz)

            anisotropicStartTangent.Y = startTangent.Y * anisotropicStartTangent.ToX0Z().Length() / startTangent.ToX0Z().Length();
              anisotropicEndTangent.Y =   endTangent.Y *   anisotropicEndTangent.ToX0Z().Length() /   endTangent.ToX0Z().Length();

            flattenedSpline.a = start;
            flattenedSpline.d = end;
            flattenedSpline.b = flattenedSpline.a + anisotropicStartTangent;
            flattenedSpline.c = flattenedSpline.d + anisotropicEndTangent;

            return flattenedSpline;
        }
        public static Bezier3 IsotropicSpline(Vector3 start, Vector3 startTangent, Vector3 end, Vector3 endTangent) => GeometryUtils.GenerateJoinSpline(start, end, startTangent, endTangent);

        public static IndexStrip GenerateSegmentSplinedUsingAlg(RoadStrip road, SplineAlgorithm algorithm) {
            var start = road.StartNode.Node.PositionProp.Value.CalcReferenceFrame();
            var end = road.EndNode.Node.PositionProp.Value.CalcReferenceFrame();
            if (road.StartNode.End == NodeEnd.Backward) start.Z *= -1;
            if (road.EndNode.End == NodeEnd.Backward) end.Z *= -1;
            var startbounds = road.StartNode.Bounds();
            var endbounds = road.EndNode.Bounds();

            var slPoint = start.O + start.X * startbounds.X;
            var srPoint = start.O + start.X * startbounds.Y;
            var elPoint = end.O + end.X * endbounds.Y;
            var erPoint = end.O + end.X * endbounds.X;

            var leftSpline = algorithm(slPoint, start.Z, elPoint, end.Z);
            var rightSpline = algorithm(srPoint, start.Z, erPoint, end.Z);

            IndexPoint sl = new(startbounds.X, leftSpline.b - slPoint);
            IndexPoint sr = new(startbounds.Y, rightSpline.b - srPoint);
            IndexPoint el = new(endbounds.Y, leftSpline.c - elPoint);
            IndexPoint er = new(endbounds.X, rightSpline.c - erPoint);

            return new(sl, sr, el, er);
        }
    }
}
