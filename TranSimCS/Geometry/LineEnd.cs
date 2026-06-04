using Microsoft.Xna.Framework;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;

namespace TranSimCS.Geometry
{
    public readonly struct LineEnd {
        public Vector3 Position { get; }
        public Vector3 Tangential { get; }
        public Vector3 Normal { get; }
        public Vector3 Lateral { get; }
        public Ray Ray => new Ray(Position, Tangential);

        public LineEnd(Vector3 position, Vector3 tangential, Vector3 normal, Vector3 lateral) {
            Position = position;
            Tangential = tangential;
            Normal = normal;
            Lateral = lateral;
        }

    public static LineEnd calcLineEnd(RoadNodeEnd node, float offset)
        => calcLineEnd(node.Node, offset, node.End);

        public static (LineEnd, LineEnd) calcBoundingLineEnds(RoadNodeEnd node) {
            var bounds = node.Bounds();
            var leftEnd = calcLineEnd(node, bounds.Min);
            var rightEnd = calcLineEnd(node, bounds.Max);
            if (node.End == NodeEnd.Backward)
                (leftEnd, rightEnd) = (rightEnd, leftEnd);
            return (leftEnd, rightEnd);
        }
        public static LineEnd calcBoundingLineEndFaced(RoadNodeEnd node, int discriminator = 1) {
            var (Min, Max, LocalLeft, localRight) = node.Bounds();
            if (discriminator < 0)
                return calcLineEnd(node, LocalLeft);
            return calcLineEnd(node, localRight);
        }

        public static LineEnd calcLineEnd(IPosition node, float offset, NodeEnd end) {
            Transform3 nodeTransform = node.PositionData.CalcReferenceFrame();
            Vector3 nodePosition = nodeTransform.O;
            Vector3 tangential = nodeTransform.Z;
            Vector3 normal = nodeTransform.Y;
            Vector3 lateral = nodeTransform.X;
            Vector3 position = nodePosition + lateral * offset;
            if (end == NodeEnd.Backward) {
                tangential = -tangential;
                lateral = -lateral;
            }
            return new LineEnd(position, tangential, normal, lateral); // Return the end position as a Vector3
        }
    }
}
