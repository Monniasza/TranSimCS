using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Spline {
    /// <summary>
    /// A spline over quaternions, not points as usual.
    /// It allows for smooth interpolation of quaternions with target velocities
    /// </summary>
    public struct Squad {
        public Quaternion StartPos;
        public Quaternion StartControl;
        public Quaternion EndPos;
        public Quaternion EndControl;
        public Quaternion Interpolate(float t) => Interpolate(StartPos, StartControl, EndPos, EndControl, t);
        public static Quaternion Interpolate(Quaternion StartPos, Quaternion StartControl, Quaternion EndPos, Quaternion EndControl, float t) {
            Quaternion slerp1 =
                Quaternion.Slerp(StartPos, EndPos, t);
            Quaternion slerp2 =
                Quaternion.Slerp(StartControl, EndControl, t);

            float u = 2 * t * (1 - t);

            return Quaternion.Slerp(
                slerp1,
                slerp2,
                u);
        }
        public Quaternion InterpolateApprox(float t) => InterpolateApprox(StartPos, StartControl, EndPos, EndControl, t);
        public static Quaternion InterpolateApprox(Quaternion StartPos, Quaternion StartControl, Quaternion EndPos, Quaternion EndControl, float t) {
            Quaternion slerp1 =
                QuaternionMethods.Chebyshev(StartPos, EndPos, t);
            Quaternion slerp2 =
                QuaternionMethods.Chebyshev(StartControl, EndControl, t);

            float u = 2 * t * (1 - t);

            return QuaternionMethods.Chebyshev(
                slerp1,
                slerp2,
                u);
        }
    }
}
