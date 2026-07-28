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

        public OrthodistantBasis Offset(float startOffset, float endOffset) => Offset(new(startOffset, endOffset));
        public OrthodistantBasis Offset(Vector2 offset) => new OrthodistantBasis(ReferenceSpline, NormalSpline, StartEndPosition + offset);

        public Transform3 SampleFrame(float t) => SampleFrame(t, 0, 0);
        public Transform3 SampleFrame(float t, float offsetStart, float offsetEnd) => SampleFrame(t, offsetStart * Vector3.UnitX, offsetEnd * Vector3.UnitX);
        public Transform3 SampleFrame(float t, Vector3 offsetStart, Vector3 offsetEnd) {
            const float epsilon = 0.001f;
            var prevPos = SamplePosition(t, offsetStart, offsetEnd);
            var nextPos = SamplePosition(t+epsilon, offsetStart, offsetEnd);

            var velocity = (nextPos - prevPos) / epsilon;
            var sampledNormal = NormalSpline[t];

            var tangent = velocity.Normalized();
            var binormal = Vector3.Cross(sampledNormal, velocity).Normalized();
            var normal = Vector3.Cross(velocity, binormal).Normalized();

            return new(binormal, normal, tangent, prevPos);
        }

        public Vector3 SamplePosition(float t) => SamplePosition(t, 0, 0);
        public Vector3 SamplePosition(float t, float offsetStart, float offsetEnd) => SamplePosition(t, offsetStart * Vector3.UnitX, offsetEnd * Vector3.UnitX);
        public Vector3 SamplePosition(float t, Vector3 offsetStart, Vector3 offsetEnd) {
            var startPosition = offsetStart + Vector3.UnitX * StartEndPosition.X;
            var endPosition = offsetEnd + Vector3.UnitX * StartEndPosition.Y;
            var smoothstepOffset = Vector3.SmoothStep(startPosition, endPosition, t);
            var orthonormalSample = new OrthonormalBasis(ReferenceSpline, NormalSpline).Sample(t);
            return orthonormalSample.Transform(smoothstepOffset);
        }
    }
}
