using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Spline {
    public struct OrthodistantBasis {
        public Bezier3 ReferenceSpline;
        public Bezier3 NormalSpline;
        public Vector2 StartEndPosition;

        public OrthodistantBasis(Bezier3 referenceSpline, Bezier3 normalSpline, Vector2 startEndPosition) {
            ReferenceSpline = referenceSpline;
            NormalSpline = normalSpline;
            StartEndPosition = startEndPosition;
        }

        public Transform3 Sample(float t) => Sample(t, 0, 0);
        public Transform3 Sample(float t, float offsetStart, float offsetEnd) => Sample(t, offsetStart * Vector3.UnitX, offsetEnd * Vector3.UnitX);
        public Transform3 Sample(float t, Vector3 offsetStart, Vector3 offsetEnd) {
            var startPosition = offsetStart + Vector3.UnitX * StartEndPosition.X;
            var endPosition = offsetEnd + Vector3.UnitX * StartEndPosition.Y;

            //Calculate the OrthonormalBasis parameters
            var sampledPosition = ReferenceSpline[t];
            var sampledNormal = NormalSpline[t];
            var sampledTangent = ReferenceSpline.Tangential(t);
            var binormal = Vector3.Cross(sampledNormal, sampledTangent).Normalized();
            var normal = Vector3.Cross(sampledTangent, binormal).Normalized();
            var tangent = sampledTangent.Normalized();

            var distanceSmoothstep = Vector3.SmoothStep(startPosition, endPosition, t);
            var position = sampledPosition + binormal*distanceSmoothstep.X + normal*distanceSmoothstep.Y + tangent*distanceSmoothstep.Z;
            var sideVelocity = (endPosition - startPosition) * 6 * t * (1 - t);
            var linearVelocity = sampledTangent + sideVelocity;
            
            tangent = linearVelocity.Normalized();
            binormal = Vector3.Cross(sampledNormal, linearVelocity).Normalized();
            normal = Vector3.Cross(linearVelocity, binormal).Normalized();

            return new(binormal, normal, tangent, position);
        }
    }
}
