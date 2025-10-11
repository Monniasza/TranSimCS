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
        float interpolantPosition;
        float projX1;
        float projY1;
        float projX2;
        float projY2;
    }
    public class StripUnravel {
        private StripUnravelNode[] nodes;
        public static StripUnravel Unravel(Bezier3 guide, Bezier3 right, int accuracy = 32) {
            var gpoints = GeometryUtils.GenerateSplinePoints(guide, accuracy);
            var rpoints = GeometryUtils.GenerateSplinePoints(right, accuracy);

        }
        private StripUnravel(StripUnravelNode[] nodes){
            this.nodes = nodes;
        }

        public Vector3 ToSpace(Vector2 source) {

        }
    }
}
