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

        public IndexStrip(IndexPoint sl, IndexPoint sr, IndexPoint el, IndexPoint er) : this() {
            Left.Start = sl;
            Right.Start = sr;
            Left.End = el;
            Right.End = er;
        }

        public SplineFrame ToSplineFrame(RoadNodeEnd start, RoadNodeEnd end) {
            var derivedLeft = Left.Derive(start, end);
            var derivedRight = Right.Derive(start, end);

            var slTangent = (derivedLeft.b - derivedLeft.a);
            var srTangent = (derivedRight.b - derivedRight.a);
            var elTangent = (derivedLeft.c - derivedLeft.d);
            var erTangent = (derivedRight.c - derivedRight.d);

            var startRelationship = GeometryUtils.UnLerp(Left.Start.Offset, Right.Start.Offset, 0);
            var endRelationship = GeometryUtils.UnLerp(Left.End.Offset, Right.End.Offset, 0);

            var startDerivative2 = (Right.Start.Offset - Left.Start.Offset);
            var endDerivative2 = (Right.End.Offset - Left.End.Offset);
            var startDerivative = (srTangent - slTangent) / startDerivative2;
            var endDerivative = (erTangent - elTangent) / endDerivative2;
            

            var startFrame = start.Node.PositionProp.Value.CalcReferenceFrame();
            var endFrame = end.Node.PositionProp.Value.CalcReferenceFrame();

            var s0Tangent = Vector3.Lerp(slTangent, srTangent, startRelationship);
            var e0Tangent = Vector3.Lerp(elTangent, erTangent, endRelationship);            

            Bezier3 centerSpline = default;
            centerSpline.a = startFrame.O;
            centerSpline.b = startFrame.O + s0Tangent;
            centerSpline.c = endFrame.O + e0Tangent;
            centerSpline.d = endFrame.O;

            var s0Acceleration = 2 * (centerSpline.a + centerSpline.c - 2 * centerSpline.b);
            var e0Acceleration = 2 * (centerSpline.d + centerSpline.b - 2 * centerSpline.c);

            var s0Omega = Vector3.Cross(s0Tangent, s0Acceleration).Normalized();
            var e0Omega = Vector3.Cross(e0Tangent, e0Acceleration).Normalized();

            var s0YTangent = Vector3.Cross(s0Omega, startFrame.Y);
            var e0YTangent = Vector3.Cross(e0Omega, endFrame.Y);

            Bezier3 lateralSpline = derivedRight - derivedLeft;
            lateralSpline.a /= startDerivative2;
            lateralSpline.b /= startDerivative2;
            lateralSpline.c /= endDerivative2;
            lateralSpline.d /= endDerivative2;

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
        public override string? ToString() {
            return $"""
            StartLeftPos: {Left.Start.Offset} StartLeftTangent: {Left.Start.Tangent}
            StartRightPos: {Right.Start.Offset} StartRightTangent: {Right.Start.Tangent}
            EndLeftPos: {Left.End.Offset} EndLeftTangent: {Left.End.Tangent}
            EndRightPos: {Right.End.Offset} EndRightTangent: {Right.End.Tangent}
            """;
        }
    }
    public struct IndexSpline{
        public IndexPoint Start;
        public IndexPoint End;
        public Bezier3 Derive(RoadNodeEnd startNode, RoadNodeEnd endNode) {
            var start = startNode.Node.PositionProp.Value.CalcReferenceFrame();
            var end = endNode.Node.PositionProp.Value.CalcReferenceFrame();
            if (startNode.End == NodeEnd.Backward) start.Z *= -1;
            if (endNode.End == NodeEnd.Backward) end.Z *= -1;
            Bezier3 result = new();
            result.a = start.O + start.X * Start.Offset;
            result.d = end.O + end.X * End.Offset;
            result.b = result.a + Start.Tangent;
            result.c = result.d + End.Tangent;
            return result;
        }
    }
    public struct IndexPoint(float offset, Vector3 tangent) {
        public float Offset = offset;
        public Vector3 Tangent = tangent;
    }
}
