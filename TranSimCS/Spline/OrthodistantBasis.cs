using System;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Geometry.SplineFrames;

namespace TranSimCS.Spline {
    public struct OrthodistantBasis {
        public Bezier3 ReferenceSpline;
        public Bezier3 NormalSpline;
        public Vector2 StartEndPosition;

        public static OrthodistantBasis Identity => new(new(Vector3.Zero, Vector3.UnitZ), new(Vector3.UnitY), Vector2.Zero);

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

        public Vector3 UnTransform(Vector3 position, float minT = 0, float maxT = 1, int depth = 24, float tolerance = 1e-3f) {
            Vector3 pO = Vector3.Zero, vX = Vector3.Zero, vY = Vector3.Zero;

            //Describe the solution as finding a plane that intersects a point, then find X and Y.
            float midpoint = 0;
            for (int i = 0; i < depth; i++) {
                midpoint = (minT + maxT) / 2;
                var sample = SampleFrame(midpoint);
                pO = sample.O;
                vX = sample.X;
                vY = sample.Y;
                var tangential = -Vector3.Cross(vY, vX);
                var dist = SplineFrame.SignedDistance(pO, tangential, position);
                if (MathF.Abs(dist) < tolerance) {
                    //Satisfactory tolerance
                    break;
                }
                if (dist > 0) {
                    //Increase T
                    minT = midpoint;
                } else {
                    //Decrease T
                    maxT = midpoint;
                }
            }

            vX.Normalize();
            vY.Normalize();
            var d = position - pO;
            var x = Vector3.Dot(d, vX);
            var y = Vector3.Dot(d, vY);
            return new Vector3(x, y, midpoint);
        }

        public bool IsFinite() => ReferenceSpline.IsFinite() && NormalSpline.IsFinite();// && !ReferenceSpline.HasCusps();
    }
}
