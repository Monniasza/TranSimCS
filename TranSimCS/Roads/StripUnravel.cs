using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Spline;

namespace TranSimCS.Roads {
    public struct StripUnravelNode {
        public float interpolantPosition;
        public float projX1;
        public float projY1;
        public float projX2;
        public float projY2;

        public StripUnravelNode(float interpolantPosition, float projX1, float projY1, float projX2, float projY2) {
            this.interpolantPosition = interpolantPosition;
            this.projX1 = projX1;
            this.projY1 = projY1;
            this.projX2 = projX2;
            this.projY2 = projY2;
        }
    }

    public class StripUnravel {
        private StripUnravelNode[] nodes;
        private Bezier3 guideSpline;
        private Bezier3 rightSpline;
        private bool hasSplines;

        public static StripUnravel Unravel(Bezier3 guide, Bezier3 right, int accuracy = 32) {
            var gpoints = GeometryUtils.GenerateSplinePoints(guide, accuracy);
            var rpoints = GeometryUtils.GenerateSplinePoints(right, accuracy);

            var nodes = new StripUnravelNode[accuracy];

            for (int i = 0; i < accuracy; i++) {
                float t = (float)i / (accuracy - 1);
                var guidePoint = gpoints[i];
                var rightPoint = rpoints[i];

                // Create a node that maps the interpolant position to 2D projections
                nodes[i] = new StripUnravelNode(
                    t,
                    guidePoint.X, guidePoint.Z,  // Project guide point to XZ plane
                    rightPoint.X, rightPoint.Z   // Project right point to XZ plane
                );
            }

            return new StripUnravel(nodes, guide, right);
        }

        private StripUnravel(StripUnravelNode[] nodes, Bezier3 guideSpline, Bezier3 rightSpline){
            this.nodes = nodes;
            this.guideSpline = guideSpline;
            this.rightSpline = rightSpline;
            this.hasSplines = true;
        }

        /// <summary>
        /// Convert 2D coordinates to 3D space using the stored spline data
        /// </summary>
        /// <param name="source">2D coordinates where X is position along road (0-1) and Y is position across road (0-1)</param>
        /// <returns>3D world position</returns>
        public Vector3 ToSpace(Vector2 source) {
            if (nodes == null || nodes.Length == 0) {
                return Vector3.Zero;
            }

            // Clamp input values to valid range
            float alongRoad = MathHelper.Clamp(source.X, 0f, 1f);
            float acrossRoad = MathHelper.Clamp(source.Y, 0f, 1f);

            // Use the original splines for more accurate interpolation
            if (hasSplines) {
                var guidePoint = guideSpline[alongRoad];
                var rightPoint = rightSpline[alongRoad];

                // Interpolate between guide point and right point based on acrossRoad parameter
                var result = Vector3.Lerp(guidePoint, rightPoint, acrossRoad);
                return result;
            }

            // Fallback to node-based interpolation if splines are not available
            int nodeIndex = (int)(alongRoad * (nodes.Length - 1));
            nodeIndex = MathHelper.Clamp(nodeIndex, 0, nodes.Length - 1);

            var node = nodes[nodeIndex];

            // Interpolate between guide point and right point
            float x = MathHelper.Lerp(node.projX1, node.projX2, acrossRoad);
            float z = MathHelper.Lerp(node.projY1, node.projY2, acrossRoad);

            return new Vector3(x, 0f, z); // Assuming Y=0 for ground level
        }

        /// <summary>
        /// Convert 2D coordinates to 3D space with additional height offset
        /// </summary>
        /// <param name="source">2D coordinates where X is position along road (0-1) and Y is position across road (0-1)</param>
        /// <param name="heightOffset">Additional height to add to the result</param>
        /// <returns>3D world position with height offset</returns>
        public Vector3 ToSpace(Vector2 source, float heightOffset) {
            var result = ToSpace(source);
            result.Y += heightOffset;
            return result;
        }

        /// <summary>
        /// Get the tangent direction at a specific position along the road
        /// </summary>
        /// <param name="t">Position along the road (0-1)</param>
        /// <returns>Normalized tangent vector</returns>
        public Vector3 GetTangentAt(float t) {
            if (!hasSplines) {
                return Vector3.Forward; // Default tangent if splines not available
            }

            t = MathHelper.Clamp(t, 0f, 1f);

            // Calculate tangent by taking derivative of the guide spline
            float dt = 0.001f; // Small delta for numerical derivative
            float t1 = Math.Max(0f, t - dt);
            float t2 = Math.Min(1f, t + dt);

            var p1 = guideSpline[t1];
            var p2 = guideSpline[t2];

            var tangent = p2 - p1;
            if (tangent.Length() > 0) {
                tangent.Normalize();
            }

            return tangent;
        }

        /// <summary>
        /// Get the width of the strip at a specific position along the road
        /// </summary>
        /// <param name="t">Position along the road (0-1)</param>
        /// <returns>Width of the strip at position t</returns>
        public float GetWidthAt(float t) {
            if (!hasSplines) {
                return 1f; // Default width if splines not available
            }

            t = MathHelper.Clamp(t, 0f, 1f);

            var guidePoint = guideSpline[t];
            var rightPoint = rightSpline[t];

            return Vector3.Distance(guidePoint, rightPoint);
        }
    }
}
