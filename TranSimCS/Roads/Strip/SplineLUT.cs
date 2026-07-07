using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Spline;

namespace TranSimCS.Roads.Strip {
    public sealed class SplineLUT {
        /// <summary>
        /// Positions and T-parameters by arc-length in forward
        /// </summary>
        public LUT ForwardLUT { get; private set; }
        /// <summary>
        /// Positions and T-parameters by arc-length in reverse
        /// </summary>
        public LUT ReverseLUT { get; private set; }
        /// <summary>
        /// (Forward, Reverse, Empty, Empty) arc-lengths in forward
        /// </summary>
        public LUT ByT { get; private set; }
        public Bezier3 Spline { get; private set; }
        public float Length { get; private set; }

        public SplineLUT(Bezier3 b, int accuracy = 65) {
            Spline = b;

            var reverse = new LUTKey[accuracy];
            var forward = new LUTKey[accuracy];
            var tToArcLength = new LUTKey[accuracy];

            var sampledPoints = GeometryUtils.GenerateSplinePoints(b, accuracy);

            forward[0] = new(0, sampledPoints[0], 0);
            float cumulativeDistance = 0;
            var step = 1.0f / (accuracy - 1);
            for(int i = 1; i < accuracy; i++) {
                var current = sampledPoints[i];
                var prev = sampledPoints[i - 1];
                var dist = Vector3.Distance(prev, current);
                cumulativeDistance += dist;
                forward[i] = new(cumulativeDistance, current, step*i);
            }

            for (int i = 0; i < accuracy; i++) {
                int j = (accuracy - i) - 1;
                var node = forward[j];
                node.X = cumulativeDistance - node.X;
                reverse[i] = node;

                var t = step * j;
                var reverseArcLength = reverse[i].X;
                var forwardArcLength = forward[j].X;

                var tToArcKey = new LUTKey(t, new Vector4(forwardArcLength, reverseArcLength, 0, 0));
                tToArcLength[i] = tToArcKey;
            }

            ForwardLUT = new LUT(forward);
            ReverseLUT = new LUT(reverse);
            ByT = new LUT(tToArcLength);
            Length = cumulativeDistance;
            
        }
    }
}
