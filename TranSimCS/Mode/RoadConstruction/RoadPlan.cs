using System.Numerics;

namespace TranSimCS.SilkNet.RoadConstruction {
    public class RoadPlan {
        public Vector3 startTangent;
        public Vector3 startPos;
        public Vector3 startLateral;

        public Vector3 endTangent;
        public Vector3 endPos;
        public Vector3 endLateral;

        public SilkNetTest menu;

        public void Align(Alignment alignment, float width) {
            var calculatedAlignments = alignment.GetAlignments();
            var moveRight = calculatedAlignments.r - 0.5f;
            startPos += moveRight * startLateral * width;
            endPos += moveRight * endLateral * width;
        }
    }
}
