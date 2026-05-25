using Microsoft.Xna.Framework;
using System;

namespace TranSimCS.Spline {
    public static class Bezier3Extensions {

        /// <summary>
        /// Straight-line distance between start and end points.
        /// </summary>
        public static float ChordLength(this Bezier3 spline) {
            return Vector3.Distance(spline.a, spline.d);
        }

        /// <summary>
        /// Length of the control polygon.
        /// Useful as an upper-bound estimate.
        /// </summary>
        public static float ControlPolygonLength(this Bezier3 spline) {
            return
                Vector3.Distance(spline.a, spline.b) +
                Vector3.Distance(spline.b, spline.c) +
                Vector3.Distance(spline.c, spline.d);
        }

        /// <summary>
        /// Numerically approximates the spline arc length
        /// using line-segment sampling.
        /// </summary>
        public static float ArcLength(this Bezier3 spline, int subdivisions = 32) {
            if (subdivisions < 1)
                throw new ArgumentOutOfRangeException(nameof(subdivisions));

            float length = 0f;

            Vector3 prev = spline[0f];

            for (int i = 1; i <= subdivisions; i++) {
                float t = i / (float)subdivisions;

                Vector3 current = spline[t];

                length += Vector3.Distance(prev, current);

                prev = current;
            }

            return length;
        }
    }
}
