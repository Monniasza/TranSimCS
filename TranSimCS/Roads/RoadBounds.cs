using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Roads {
    public struct RoadBounds {
        public float leftStart = float.PositiveInfinity;
        public float rightStart = float.NegativeInfinity;
        public float leftEnd = float.PositiveInfinity;
        public float rightEnd = float.NegativeInfinity;

        public RoadBounds() { }
        public RoadBounds(float leftStart, float rightStart, float leftEnd, float rightEnd) {
            this.leftStart = leftStart;
            this.rightStart = rightStart;
            this.leftEnd = leftEnd;
            this.rightEnd = rightEnd;
        }

        public RoadBounds Update(float start, float end) {
            return new RoadBounds(
                MathF.Min(start, leftStart), MathF.Max(start, rightStart),
                MathF.Min(end, leftEnd), MathF.Max(end, rightEnd)
            );
        }
    }
}
