using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LanguageExt.Common;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Render;

namespace TranSimCS.Spline {
    /// <summary>
    /// A Boundary-Constrained Rotation-Minimizing Frame (BCRMF).
    /// 
    /// </summary>
    public sealed class RotationFrameLUT {
        //Contents
        /// <summary>
        /// A set of <see cref="ConstructionParameters"/> used to create this <see cref="RotationFrameLUT"/>
        /// </summary>
        public ConstructionParameters Parameters { get; private set; }
        /// <summary>
        /// An <see cref="ImmutableArray{RotationFrameSample}"/> of <see cref="RotationFrameSample"/>s
        /// </summary>
        public ImmutableArray<RotationFrameSample> Data { get; private set; }

        //Derived properties
        public Bezier3 CenterSpline => Parameters.Spline;
        public float TotalLength => Data[^1].Distance;

        //Lookups
        /*public RotationFrameSample FindByT(float t) {
            var binarySearch = BinarySearchByT(t, out var left, out var right, out _);
        }*/
        /// <summary>
        /// Finds two <see cref="RotationFrameSample"/>s that the spline parameter <paramref name="t"/> is located between.
        /// If <paramref name="t"/> is below 0, the algorithm will return 2 first samples and a negative interpolation coefficient.
        /// If <paramref name="t"/> is above 1, the algorithm will return 2 last samples and a interpolation coefficient above 1.
        /// The target sample is: 
        /// </summary>
        /// <param name="t">target spline parameter <see cref="RotationFrameSample.T">T</see> of the <see cref="RotationFrameSample"/></param>
        /// <param name="min">set to the last <see cref="RotationFrameSample"/> with <see cref="RotationFrameSample.T">T</see> less or equal to <paramref name="t"/></param>
        /// <param name="max">set to the smallest <see cref="RotationFrameSample"/> with <see cref="RotationFrameSample.T">T</see> above <paramref name="t"/></param>
        /// <param name="startIndex">set to the array index of the last <see cref="RotationFrameSample"/> with less or equal <see cref="RotationFrameSample.T">T</see> than <paramref name="t"/></param>
        /// <returns></returns>
        public float BinarySearchByT(float t, out RotationFrameSample min, out RotationFrameSample max, out int startIndex) {
            int minIdx = 0;
            int maxIdx = Data.Length - 1;
            startIndex = -1;
            while (minIdx <= maxIdx) {
                var midIdx = (minIdx + maxIdx) >> 1;
                RotationFrameSample sample = Data[midIdx];
                if(sample.T == t) {
                    //Exact match
                    min = sample;
                    max = sample;
                    startIndex = midIdx;
                    return 0;
                }else if(sample.T < t) {
                    //Midpoint sample is below the target
                    minIdx = midIdx + 1;
                    startIndex = midIdx;
                } else {
                    //Midpoint sample is above the target
                    maxIdx = midIdx - 1;
                }
            }
            var index = startIndex;
            if (index < 0) index = 0;
            if (index > Data.Length - 2) index = Data.Length - 2;
            min = Data[index];
            max = Data[index + 1];
            return GeometryUtils.UnLerp(min.T, max.T, t);
        }

        internal RotationFrameLUT(ImmutableArray<RotationFrameSample> data, ConstructionParameters parameters) {
            Debug.Assert(data != null, "data is null");
            Debug.Assert(parameters.Spline.a.IsFinite(), "invalid spline start");
            Debug.Assert(parameters.Spline.a.IsFinite(), "invalid spline start control");
            Debug.Assert(parameters.Spline.a.IsFinite(), "invalid spline end control");
            Debug.Assert(parameters.Spline.a.IsFinite(), "invalid spline start");
            this.Parameters = parameters;
            this.Data = data;
        }
        public struct ConstructionParameters {
            public Bezier3 Spline;
            public float MaxSegmentLength = 1;
            public float MaxAngle = MathHelper.ToRadians(10);
            public float MaxDeviation = 0.1f;
            public Vector3 startNormal;
            public Vector3 endNormal;
            public float startTwistRate;
            public float endTwistRate;

            public ConstructionParameters() { }
        }
        public struct Hermite1 {
            public float StartValue;
            public float EndValue;
            public float StartDerivative;
            public float EndDerivative;

            public float Value(float t)
                => MathHelper.Hermite(StartValue, StartDerivative, EndValue, EndDerivative, t);
            public float Derivative(float t) {
                var coeff13 = 6 * t * (1 + t);
                var coeff2 = ((t * 3) - 4) * t + 1;
                var coeff4 = (3 * t - 2) * t;
                return StartValue * coeff13
                     + StartDerivative * coeff2
                     - EndValue * coeff13
                     + EndDerivative * coeff4;
            }
        }
        public struct RotationFrameSample {
            // Arc-length domain
            public float Distance;      // Cumulative distance
            public float T;             // Bezier parameter

            // Geometry cache
            public Vector3 Position;    // Optional
            public Vector3 Tangent;     // Optional
            public Vector3 Lateral;
            public Vector3 Normal;

            // Orientation
            public Quaternion Rotation;

            // d(twist)/ds in radians per meter
            public float TwistRate;
            public Quaternion SquadControl;

            public static RotationFrameSample Start(Vector3 position, Vector3 tangent, Vector3 normal, float startTwistRate) {
                var binormal = Vector3.Cross(tangent, normal).Normalized();
                normal = Vector3.Cross(tangent, binormal).Normalized();
                return new() {
                    Distance = 0,
                    T = 0,
                    Position = position,
                    Tangent = tangent,
                    Rotation = GeometryUtils.QuaternionFromBasisVectors(binormal, normal, tangent),
                    TwistRate = startTwistRate,
                    Lateral = binormal,
                    Normal = normal,
                };
            }
            public static RotationFrameSample Transport(RotationFrameSample sample, Vector3 nextTangent, Vector3 nextPos, float newT) {
                var prevPos = sample.Position;
                var prevTangent = sample.Tangent;
                var prevNormal = sample.Normal;
                var nextNormal = GeometryUtils.DoubleReflection(prevTangent, nextTangent, prevPos, nextPos, prevNormal);
                var nextBinormal = Vector3.Cross(nextTangent, nextNormal).Normalized();
                nextNormal = Vector3.Cross(nextTangent, nextBinormal).Normalized();
                var nextQuaternion = GeometryUtils.QuaternionFromBasisVectors(nextBinormal, nextNormal, nextTangent);
                var nextDistance = sample.Distance + Vector3.Distance(prevPos, nextPos);
                return new() {
                    Distance = nextDistance,
                    T = newT,
                    Position = nextPos,
                    Tangent = nextTangent,
                    Rotation = nextQuaternion,
                    Lateral = nextBinormal,
                    Normal = nextNormal,
                    TwistRate = float.NaN,
                };
            }

        }

        public static RotationFrameLUT Construct(ConstructionParameters parameters) {
            //Stage 0: validation
            //TODO

            //Stage 0.5: initialization
            var angleCosLimit = MathF.Cos(parameters.MaxAngle);

            //Stage 1: Subdivision
            var subdivisionBuilder = DLNode<Vector4>.CreateLinear(new(parameters.Spline.a, 0), new(parameters.Spline.d, 1));
            bool goBack = false;
            for (var iterator = subdivisionBuilder; iterator.Next != null; iterator = goBack ? iterator : iterator.Next) {
                var next = iterator.Next;
                var midT = (iterator.val.W + next.val.W) / 2;
                var midpoint = parameters.Spline[midT];
                var prevPos = iterator.val.ToXYZ();
                var nextPos = next.val.ToXYZ();
                if (
                    Vector3.Distance(prevPos, nextPos) > parameters.MaxSegmentLength || //segment length
                    -GeometryUtils.CosBetweenLines(prevPos, midpoint, nextPos) < angleCosLimit || //angle
                    GeometryUtils.DistanceToLine(prevPos, nextPos, midpoint) > parameters.MaxDeviation //deviation
                ) {
                    var midNode = new DLNode<Vector4>(new(midpoint, midT));
                    midNode.Prev = iterator;
                    midNode.Next = next;
                    goBack = true;
                }
            }
            var subdivision = subdivisionBuilder.IterateValuesNext().ToArray();

            //Stage 2: Rotation minimizing transport
            var result = new RotationFrameSample[subdivision.Length];
            result[0] = RotationFrameSample.Start(parameters.Spline.a, parameters.Spline.Tangential(0), parameters.startNormal, parameters.startTwistRate);
            for (int i = 1; i < subdivision.Length; i++) {
                var nextT = subdivision[i].W;
                var nextPos = subdivision[i].ToXYZ();
                var nextTangent = parameters.Spline.Tangential(nextT);
                var prevSample = result[i - 1];
                var nextSample = RotationFrameSample.Transport(prevSample, nextTangent, nextPos, nextT);
                result[i] = nextSample;
            }

            //Stage 3: Match the end normals and twist rates
            var transportedNormal = result[^1].Normal;
            var targetNormal = parameters.endNormal;
            var frameToEndAngle = GeometryUtils.SignedAngle(parameters.Spline.Tangential(1), transportedNormal, targetNormal);
            var totalLength = result[^1].Distance;
            Hermite1 angleCorrectionHermite = new() {
                StartValue = 0,
                EndValue = frameToEndAngle,
                StartDerivative = parameters.startTwistRate * totalLength,
                EndDerivative = parameters.endTwistRate * totalLength,
            };
            for (int i = 0; i < result.Length; i++) {
                var sample = result[i];

                //Compute instantenous spline derivative and angular velocity
                var tangent = sample.Tangent;
                var acceleration = parameters.Spline.Acceleration(sample.T);
                var angularVelocity = Vector3.Cross(tangent, acceleration) / tangent.LengthSquared();

                //Calculate angle corrections
                var s = sample.Distance / totalLength;
                var angleCorrection = angleCorrectionHermite.Value(s);
                var twistRate = angleCorrectionHermite.Derivative(s) / totalLength;
                
                Quaternion correctionQuaternion = Quaternion.CreateFromAxisAngle(tangent, angleCorrection);
                sample.Rotation = correctionQuaternion * sample.Rotation;

                //Calculate the Squad control point
                float interval = 0;
                if (i > 0) interval += sample.Distance - result[i - 1].Distance;
                if (i < result.Length - 1) interval += result[i + 1].Distance - sample.Distance;
                var squad = CreateSquadControl(sample.Rotation, angularVelocity, interval / 2);
                sample.SquadControl = squad;

                //Reconstruct cached vectors
                var matrix = Matrix.CreateFromQuaternion(sample.Rotation);
                sample.Normal = matrix.Up;
                sample.Lateral = matrix.Right;
                result[i] = sample;
            }

            return new(result.ToImmutableArray(), parameters);
            //Stage 4: Quaternion LUT
            
        }

        /// <summary>
        /// Computes the Squad control quaternion for a rotation sample from its
        /// instantaneous angular velocity.
        /// </summary>
        /// <param name="rotation">
        /// The orientation quaternion at this sample.
        /// </param>
        /// <param name="angularVelocity">
        /// The instantaneous angular velocity of the frame in radians per unit
        /// distance. The vector direction is the axis of rotation and the magnitude
        /// is the rotation rate.
        /// </param>
        /// <param name="distance">
        /// The distance interval over which this control tangent is applied.
        /// Usually the distance to the neighboring sample.
        /// </param>
        /// <returns>
        /// A quaternion control point suitable for Squad interpolation.
        /// </returns>
        /// <remarks>
        /// Squad control points represent the tangent of a quaternion curve.
        /// This method converts angular velocity into an equivalent quaternion
        /// tangent using:
        ///
        ///     control = rotation * exp(-angularVelocity * distance / 4)
        ///
        /// The negative sign follows the Shoemake Squad convention. The distance
        /// factor converts angular velocity (radians per metre) into a finite
        /// rotation over the interpolation interval.
        ///
        /// The angular velocity must be expressed in the same coordinate convention
        /// as the quaternion multiplication order used by the interpolation code.
        /// </remarks>
        public static Quaternion CreateSquadControl(
            Quaternion rotation,
            Vector3 angularVelocity,
            float distance) {
            float magnitude = angularVelocity.Length();

            if (magnitude < 1e-6f)
                return rotation;

            float angle = magnitude * distance * 0.25f;
            Vector3 axis = angularVelocity / magnitude;

            Quaternion correction =
                Quaternion.CreateFromAxisAngle(axis, -angle);

            return correction * rotation;
        }

        //Interpolation
        public static RotationFrameSample Interpolate(Bezier3 spline, RotationFrameSample a, RotationFrameSample b, float t, InterpolationQuality quality = InterpolationQuality.Normal) {
            RotationFrameSample result = default;
            result.Distance = MathHelper.Lerp(a.Distance, b.Distance, t);
            result.T = MathHelper.Lerp(a.T, b.T, t);
            result.Position = spline[result.T];
            var span = b.Distance - a.Distance;
            Hermite1 twistHermite = new() {
                EndDerivative = b.TwistRate * span,
                EndValue = 0,
                StartDerivative = a.TwistRate * span,
                StartValue = 0,
            };

            //Pick an interpolation algorithm for angles
            switch (quality) {
                case InterpolationQuality.Fast:
                    result.Rotation = Quaternion.Lerp(a.Rotation, b.Rotation, t).Normalized();
                    break;
                case InterpolationQuality.Normal:
                    result.Rotation = QuaternionMethods.Chebyshev(a.Rotation, b.Rotation, t).Normalized();
                    break;
                case InterpolationQuality.High:
                    result.Rotation = Squad.InterpolateApprox(a.Rotation, a.SquadControl, b.Rotation, b.SquadControl, t);
                    break;
                case InterpolationQuality.Extreme:
                    result.Rotation = Squad.Interpolate(a.Rotation, a.SquadControl, b.Rotation, b.SquadControl, t);
                    break;
                default:
                    throw new ArgumentException(nameof(quality));
            }
            result.TwistRate = twistHermite.Derivative(t);
            return result;
        }
    }
}
