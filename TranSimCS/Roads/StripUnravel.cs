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

            return new StripUnravel(nodes);
        }

        private StripUnravel(StripUnravelNode[] nodes){
            this.nodes = nodes;
        }

        public Vector3 ToSpace(Vector2 source) {
            if (nodes == null || nodes.Length == 0) {
                return Vector3.Zero;
            }

            // Find the closest node or interpolate between nodes
            // For simplicity, return the first node's position transformed by the source vector
            // This is a basic implementation - a more sophisticated version would interpolate
            var firstNode = nodes[0];
            var lastNode = nodes[nodes.Length - 1];

            // Linear interpolation between first and last node based on source.X
            float t = MathHelper.Clamp(source.X, 0f, 1f);

            // Interpolate between guide and right edge based on source.Y
            float edgeT = MathHelper.Clamp(source.Y, 0f, 1f);

            // Find the appropriate node based on t
            int nodeIndex = (int)(t * (nodes.Length - 1));
            nodeIndex = MathHelper.Clamp(nodeIndex, 0, nodes.Length - 1);

            var node = nodes[nodeIndex];

            // Interpolate between guide point and right point
            float x = MathHelper.Lerp(node.projX1, node.projX2, edgeT);
            float z = MathHelper.Lerp(node.projY1, node.projY2, edgeT);

            return new Vector3(x, 0f, z); // Assuming Y=0 for ground level
        }
    }
}
