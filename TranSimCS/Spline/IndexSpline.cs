using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Spline {
    public struct IndexStrip{
        public IndexSpline Left;
        public IndexSpline Right;
        public SplineFrame ToSplineFrame(RoadNodeEnd start, RoadNodeEnd end) {
            var derivedLeft = Left.Derive(start, end);
            var derivedRight = Right.Derive(start, end);

            var slTangent = (derivedLeft.b - derivedLeft.a);
            var srTangent = (derivedRight.b - derivedRight.a);
            var elTangent = (derivedLeft.c - derivedRight.d);
            var erTangent = (derivedRight.c - derivedRight.d);

            var startDerivative = (slTangent - srTangent) / (Left.Start.Offset - Right.Start.Offset);
            var endDerivative = (elTangent - erTangent) / (Left.End.Offset - Right.End.Offset);

            var startRelationship = GeometryUtils.UnLerp(Left.Start.Offset, Right.Start.Offset, 0);
            var endRelationship = GeometryUtils.UnLerp(Left.End.Offset, Right.End.Offset, 0);

            var startFrame = start.CalcReferenceFrame();
            var endFrame = end.CalcReferenceFrame();

            var s0Tangent = Vector3.Lerp(slTangent, srTangent, startRelationship);
            var e0Tangent = Vector3.Lerp(elTangent, erTangent, endRelationship);            

            Bezier3 centerSpline = default;
            centerSpline.a = startFrame.O;
            centerSpline.b = startFrame.O + s0Tangent;
            centerSpline.c = endFrame.O + e0Tangent;
            centerSpline.d = endFrame.O;

            var s0Acceleration = 2 * (centerSpline.a + centerSpline.c - 2*centerSpline.b);
            var e0Acceleration = 2 * (centerSpline.d + centerSpline.b - 2 * centerSpline.c);

            var s0Omega = Vector3.Cross(s0Tangent, s0Acceleration).Normalized();
            var e0Omega = Vector3.Cross(e0Tangent, e0Acceleration).Normalized();

            var s0XTangent = Vector3.Cross(s0Omega, startFrame.X);
            var e0XTangent = Vector3.Cross(e0Omega, endFrame.X);

            var s0YTangent = Vector3.Cross(s0Omega, startFrame.Y);
            var e0YTangent = Vector3.Cross(e0Omega, startFrame.Y);

            Bezier3 lateralSpline = default;
            lateralSpline.a = startFrame.X;
            lateralSpline.b = startFrame.X + s0XTangent;
            lateralSpline.c = endFrame.X + e0XTangent;
            lateralSpline.d = endFrame.X;

            Bezier3 normalSpline = default;
            normalSpline.a = startFrame.Y;
            normalSpline.b = startFrame.Y + s0YTangent;
            normalSpline.c = endFrame.Y + e0YTangent;
            normalSpline.d = endFrame.Y;

            SplineFrame result = new SplineFrame();
            result.CenterSpline = centerSpline;
            result.XPlusSpline = lateralSpline;
            result.YPlusSpline = normalSpline;
            return result;
        }
    
    }
    public struct IndexSpline{
        public IndexPoint Start;
        public IndexPoint End;
        public Bezier3 Derive(RoadNodeEnd startNode, RoadNodeEnd endNode) {
            var startRef = startNode.CalcReferenceFrame();
            var endRef = endNode.CalcReferenceFrame();
            Bezier3 result = new();
            result.a = startRef.O + startRef.X * Start.Offset;
            result.d = endRef.O + endRef.X * End.Offset;
            result.b = result.a + Start.Tangent;
            result.c = result.d + End.Tangent;
            return result;
        }
    }
    public struct IndexPoint(float offset, Vector3 tangent) {
        public float Offset;
        public Vector3 Tangent;
    }
}
