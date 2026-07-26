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

namespace TranSimCS.Spline {
    public struct IndexSpline{
        public IndexPoint Start;
        public IndexPoint End;

        public IndexSpline(IndexPoint start, IndexPoint end) {
            Start = start;
            End = end;
        }

        public Bezier3 Derive(HalfNode startNode, HalfNode endNode) {
            var start = startNode.Cache.ReferenceFrame;
            var end = endNode.Cache.ReferenceFrame;
            Bezier3 result = new();
            result.a = start.O + start.X * Start.Offset;
            result.d = end.O + end.X * End.Offset;
            result.b = result.a + Start.Tangent;
            result.c = result.d + End.Tangent;
            return result;
        }

        public OrthodistantBasis ToOrthodistantBasis(HalfNode startNode, HalfNode endNode) {
            //Derive the index splines
            var positionSpline = Derive(startNode, endNode);

            //Calculate the Y spline
            var startYVector = startNode.Cache.ReferenceFrame.Y;
            var endYVector = endNode.Cache.ReferenceFrame.Y;

            Bezier3 ySpline = new(
                startYVector,
                startYVector,
                endYVector,
                endYVector
            );

            return new OrthodistantBasis(positionSpline, ySpline, new(-Start.Offset, -End.Offset));
        }
    }
    public struct IndexPoint(float offset, Vector3 tangent) {
        public float Offset = offset;
        public Vector3 Tangent = tangent;
    }
}
