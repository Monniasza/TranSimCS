using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Spline {
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
    }
}
