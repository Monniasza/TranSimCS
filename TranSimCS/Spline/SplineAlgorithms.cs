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

        public static IndexSpline GenerateSegmentSplinedUsingAlg(RoadStrip road, SplineAlgorithm algorithm) {
            var start = road.StartNode.Cache.ReferenceFrame;
            var end = road.EndNode.Cache.ReferenceFrame;

            var roadBounds = road.Bounds;
            var startT = roadBounds.startRange.Middle();
            var endT = roadBounds.endRange.Middle();

            var startPoint = start.O + start.X * startT;
            var endPoint = end.O + end.X * endT;

            var generatedSpline = algorithm(startPoint, start.Z, endPoint, end.Z);

            IndexPoint startIndexPoint = new(startT, generatedSpline.b - generatedSpline.a);
            IndexPoint endIndexPoint = new(endT, generatedSpline.c - generatedSpline.d);

            //Test for NaN values
            if (!float.IsFinite(startT)) throw new ArgumentException("start offset");
            if (!float.IsFinite(endT)) throw new ArgumentException("end offset");
            VectorMethods.CheckVector(startIndexPoint.Tangent, "start.Tangent");
            VectorMethods.CheckVector(endIndexPoint.Tangent, "end.Tangent");

            return new(startIndexPoint, endIndexPoint);
        }
    }
}
