using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Roads {
    public class SplineTransformer {
        /// <summary>
        /// Transform a vector using the spline frame at the beginning of the road (t=0)
        /// </summary>
        public static Vector3 Transform(Vector3 vector, SplineFrame splineFrame) {
            return Transform(vector, splineFrame, 0f);
        }

        /// <summary>
        /// Transform a vector using the spline frame at a specific position along the road
        /// </summary>
        /// <param name="vector">The relative position vector to transform</param>
        /// <param name="splineFrame">The spline frame defining the coordinate system</param>
        /// <param name="t">Position along the spline (0 = start, 1 = end)</param>
        /// <returns>Transformed world position</returns>
        public static Vector3 Transform(Vector3 vector, SplineFrame splineFrame, float t) {
            // Clamp t to valid range
            t = MathHelper.Clamp(t, 0f, 1f);

            // Transform the vector using the spline frame's coordinate system at position t
            // This transforms a relative position vector into world coordinates using the spline frame
            var result = splineFrame.CenterSpline[t] +
                        vector.X * splineFrame.XPlusSpline[t] +
                        vector.Y * splineFrame.YPlusSpline[t] +
                        vector.Z * splineFrame.ZPlusSpline[t];
            return result;
        }

        /// <summary>
        /// Transform multiple vectors along the spline at different positions
        /// </summary>
        /// <param name="vectors">Array of relative position vectors</param>
        /// <param name="splineFrame">The spline frame defining the coordinate system</param>
        /// <param name="tValues">Array of t values corresponding to each vector (0 = start, 1 = end)</param>
        /// <returns>Array of transformed world positions</returns>
        public static Vector3[] Transform(Vector3[] vectors, SplineFrame splineFrame, float[] tValues) {
            if (vectors.Length != tValues.Length) {
                throw new ArgumentException("Vectors and tValues arrays must have the same length");
            }

            var results = new Vector3[vectors.Length];
            for (int i = 0; i < vectors.Length; i++) {
                results[i] = Transform(vectors[i], splineFrame, tValues[i]);
            }
            return results;
        }
    }
}
