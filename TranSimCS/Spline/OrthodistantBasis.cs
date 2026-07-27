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
            var sampledVelocity = ReferenceSpline.Tangential(t);
            var binormal = Vector3.Cross(sampledNormal, sampledVelocity).Normalized();
            var normal = Vector3.Cross(sampledVelocity, binormal).Normalized();
            var tangent = sampledVelocity.Normalized();
            const float epsilon = 0.001f;
            var nextTangent = ReferenceSpline.Tangential(t + epsilon);
            var nextNormal = NormalSpline[t + epsilon];
            var nextBinormal = Vector3.Cross(nextNormal, nextTangent).Normalized();
            var binormalVelocity = (nextBinormal - binormal) / epsilon;
            var tangentVelocity = (nextTangent - tangent) / epsilon;
            var normalVelocity = (nextNormal - normal) / epsilon;

            var distanceSmoothstep = Vector3.SmoothStep(startPosition, endPosition, t);
            var offset = binormal * distanceSmoothstep.X + normal * distanceSmoothstep.Y + tangent * distanceSmoothstep.Z;
            var position = sampledPosition + offset;
            var sideVelocity = (endPosition - startPosition) * 6 * t * (1 - t);
            var frameVelocity = binormalVelocity * distanceSmoothstep.X + normalVelocity*distanceSmoothstep.Y + tangentVelocity*distanceSmoothstep.Z;
            var linearVelocity = sampledVelocity + sideVelocity + frameVelocity;
            
            tangent = linearVelocity.Normalized();
            binormal = Vector3.Cross(sampledNormal, linearVelocity).Normalized();
            normal = Vector3.Cross(linearVelocity, binormal).Normalized();

            return new(binormal, normal, tangent, position);
        }
    }
}
