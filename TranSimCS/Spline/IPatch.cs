using System.Numerics;

namespace TranSimCS.Spline {
    public interface IPatch {
        public Vector3 Get(float x, float y);
        public IPatch SubRange(float x0, float y0, float x1, float y1);
    }
}
