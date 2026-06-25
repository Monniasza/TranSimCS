using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Model;

namespace TranSimCS.Geometry {
    public static class QuadrilateralVsPlane {
        private const float Epsilon = 1e-6f;

        /// <summary>
        /// Intersects a quadrilateral and a plane: The possible results are: <list type="bullet">
        ///     <item>0 items - no intersection</item>
        ///     <item>1 item - a point</item>
        ///     <item>2 items - a line segment</item>
        ///     <item>4 items - the entire quadrilateral</item>
        /// </list>
        /// </summary>
        /// <param name="quad"></param>
        /// <param name="plane"></param>
        /// <returns>intersection between a quadrilateral and a plane</returns>
        public static Vector3[] Intersect(this Quad<Vector3> quad, Plane plane) {
            Vector3[] vertices = [
                quad.A,
                quad.B,
                quad.C,
                quad.D
            ];

            float[] distances = new float[4];

            bool allOnPlane = true;

            for (int i = 0; i < 4; i++) {
                distances[i] = plane.DotCoordinate(vertices[i]);

                if (MathF.Abs(distances[i]) > Epsilon)
                    allOnPlane = false;
            }

            // Entire quadrilateral lies in the plane.
            if (allOnPlane)
                return vertices;

            List<Vector3> result = new();

            for (int i = 0; i < 4; i++) {
                int j = (i + 1) % 4;

                Vector3 p0 = vertices[i];
                Vector3 p1 = vertices[j];

                float d0 = distances[i];
                float d1 = distances[j];

                bool on0 = MathF.Abs(d0) <= Epsilon;
                bool on1 = MathF.Abs(d1) <= Epsilon;

                // Vertex lies on plane.
                if (on0)
                    AddUnique(result, p0);

                // Edge crosses plane.
                if ((d0 < -Epsilon && d1 > Epsilon) ||
                    (d0 > Epsilon && d1 < -Epsilon)) {

                    float t = d0 / (d0 - d1);

                    Vector3 intersection = Vector3.Lerp(p0, p1, t);
                    AddUnique(result, intersection);
                }

                // No need to add p1 here because it will become p0
                // during the next iteration.
            }

            return result.ToArray();
        }

        private static void AddUnique(List<Vector3> list, Vector3 point) {
            foreach (var existing in list) {
                if (Vector3.DistanceSquared(existing, point) <= Epsilon * Epsilon)
                    return;
            }

            list.Add(point);
        }
    }
}
