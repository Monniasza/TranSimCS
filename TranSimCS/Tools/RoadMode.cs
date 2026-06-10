using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Tools {
    public interface RoadMode {
        public string Name { get; }
        public void CreateValues(RoadPlan plan);
    }
    public class StraightMode : RoadMode {
        public string Name => "Straight";
        public void CreateValues(RoadPlan plan) {
            plan.endLateral = plan.startLateral;
            plan.endTangent = plan.startTangent;
            Ray ray = new Ray(plan.startPos, plan.startTangent);
            var endPos = GeometryUtils.FindNearest(ray, plan.endPos, out var _);
            plan.endPos = endPos;
        }
    }
    public class SBendMode : RoadMode {
        public string Name => "S-bend, same-direction";
        public void CreateValues(RoadPlan plan) {
            plan.endLateral = plan.startLateral;
            plan.endTangent = plan.startTangent;
        }
    }

    public class CircMode : RoadMode {
        public string Name => "Circular arc";

        public void CreateValues(RoadPlan plan) {
            var reflectionVector = plan.endPos - plan.startPos;
            reflectionVector.Normalize();

            Vector3 startNormal =
                Vector3.Normalize(
                    Vector3.Cross(
                        plan.startTangent,
                        plan.startLateral
                    )
                );

            plan.endTangent =
                -GeometryUtils.ReflectVectorByNormal(
                    plan.startTangent,
                    reflectionVector
                );

            plan.endTangent.Normalize();

            plan.endLateral =
                Vector3.Normalize(
                    Vector3.Cross(
                        startNormal,
                        plan.endTangent
                    )
                );
        }
    }
    public class FromReferenceMode : RoadMode {
        public string Name => "From the snapping grid";

        public void CreateValues(RoadPlan plan) {
            var refframe = plan.menu.configuration.SnapGrid.Position.CalcReferenceFrame();
            plan.endTangent = refframe.Z;
            plan.endLateral = refframe.X;
        }
    }
}
