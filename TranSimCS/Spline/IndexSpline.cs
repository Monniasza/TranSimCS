using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt.ClassInstances.Pred;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Geometry.SplineFrames;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;

namespace TranSimCS.Spline {
    public struct IndexSpline{
        public IndexPoint Start;
        public IndexPoint End;

        public IndexSpline(IndexPoint start, IndexPoint end) {
            Start = start;
            End = end;
        }

        public OrthodistantBasis ToOrthodistantBasis(PositionEulerAngles startNode, PositionEulerAngles endNode) {
            //Derive the index splines
            var start = startNode.CalcReferenceFrame();
            var end = endNode.CalcReferenceFrame();
            Bezier3 positionSpline = new();
            positionSpline.a = start.O + start.X * Start.Offset;
            positionSpline.d = end.O + end.X * End.Offset;
            positionSpline.b = positionSpline.a + Start.Tangent;
            positionSpline.c = positionSpline.d + End.Tangent;

            //Calculate the Y spline
            var startYVector = start.Y;
            var endYVector = end.Y;

            Bezier3 ySpline = new(
                startYVector,
                startYVector,
                endYVector,
                endYVector
            );

            return new OrthodistantBasis(positionSpline, ySpline, new(-Start.Offset, End.Offset));
        }
    }
    public struct IndexPoint(float offset, Vector3 tangent) {
        public float Offset = offset;
        public Vector3 Tangent = tangent;
    }
}
