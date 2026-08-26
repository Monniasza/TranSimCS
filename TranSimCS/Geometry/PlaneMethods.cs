using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Geometry {
    public static class PlaneMethods {
        /// <summary>
        /// Returns the signed distance from a point to the <paramref name="plane"/>.
        /// Negative values are inside the plane's half-space, positive values are outside, and zero is on the plane
        /// </summary>
        public static float SignedDistance(this Plane plane, Vector3 vector) => (Vector3.Dot(vector, plane.Normal) + plane.D) / vector.Length();

        /// <summary>
        /// Returns the signed distance from a point to the <paramref name="plane"/>. assuming the plane is normalized.
        /// If the plane is not normalized, the sign will be correct, but the value will not.
        /// Negative values are inside the plane's half-space, positive values are outside, and zero is on the plane
        /// </summary>
        public static float PrenormSignedDistance(this Plane plane, Vector3 vector) => Vector3.Dot(vector, plane.Normal) + plane.D;
    }
}
