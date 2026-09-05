using System.Numerics;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads.Strip;
using TranSimCS.Setting;

namespace TranSimCS.Tools.RoadConstruction {
    public static class SplitRoadMethods {
        public static void DrawRoadSpline(RoadStrip road, Mesh mesh, Color c, float width = 0.2f, float yoffset = 0.4f) {
            var spline = road.ToolBasis;
            var renderBin = mesh;

            //Draw the spline
            var accuracy = Settings.RoadAccuracy;
            var step = 1.0f / (accuracy - 1);
            var points = new Transform3[accuracy];
            for (int i = 0; i < accuracy; i++) {
                points[i] = spline.SampleFrame(i * step);
            }

            for (int i = 1; i < accuracy; i++) {
                var prev = points[i - 1];
                var next = points[i];
                var c0 = prev.O + yoffset * prev.Y;
                var c1 = next.O + yoffset * prev.Y;
                var normal = Vector3.Normalize(prev.Y + next.Y);

                renderBin.DrawLine(c0, c1, normal, c, width);
            }
        }
    }
}
