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
            //Derive the index splines
            var derivedLeft = Left.Derive(start, end);
            var derivedRight = Right.Derive(start, end);

            //Find where the node's 0 is
            var startZeroT = GeometryUtils.UnLerp(Left.Start.Offset, Right.Start.Offset, 0);
            var endZeroT = GeometryUtils.UnLerp(Left.End.Offset, Right.End.Offset, 0);
            Bezier3 zeroSpline = Bezier3.BiLerp(derivedLeft, derivedRight, startZeroT, endZeroT);

            //Calculate start and end width
            var startWidth = (Right.Start.Offset - Left.Start.Offset);
            var endWidth = (Right.End.Offset - Left.End.Offset);

            //Calculate the X spline
            var right2leftSpline = derivedRight - derivedLeft;
            var xSpline = new Bezier3(
                right2leftSpline.a / startWidth,
                right2leftSpline.b / startWidth,
                right2leftSpline.c / endWidth,
                right2leftSpline.d / endWidth
            );

            var startReferenceFrame = start.CalcReferenceFrame();
            var endReferenceFrame = end.CalcReferenceFrame();

            //Calculate the Y spline
            var startYVector = startReferenceFrame.Y;
            var endYVector = endReferenceFrame.Y;

            Bezier3 ySpline = new(
                startYVector,
                startYVector,
                endYVector,
                endYVector
            );

            SplineFrame result = new SplineFrame();
            result.CenterSpline = zeroSpline;
            result.XPlusSpline = xSpline;
            result.YPlusSpline = ySpline;
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
