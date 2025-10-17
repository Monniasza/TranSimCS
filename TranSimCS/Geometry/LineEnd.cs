using Microsoft.Xna.Framework;
using TranSimCS.Roads;
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
            if (node.Node.Lanes.Count == 0) {
                var leftEnd0 = calcLineEnd(node, 0);
                return (leftEnd0, leftEnd0);
            }
            var leftEnd = calcLineEnd(node, node.Node.Lanes[0].LeftPosition);
            var rightEnd = calcLineEnd(node, node.Node.Lanes[node.Node.Lanes.Count - 1].RightPosition);
            if (node.End == NodeEnd.Backward)
                (leftEnd, rightEnd) = (rightEnd, leftEnd);
            return (leftEnd, rightEnd);
        }
        public static LineEnd calcBoundingLineEndFaced(RoadNodeEnd node, int discriminator = 1) {
            if (node.Node.Lanes.Count == 0) {
                var leftEnd0 = calcLineEnd(node, 0);
                return leftEnd0;
            }
            if (discriminator < 0 ^ node.End == NodeEnd.Backward)
                return calcLineEnd(node, node.Node.Lanes[0].LeftPosition);
            return calcLineEnd(node, node.Node.Lanes[node.Node.Lanes.Count - 1].RightPosition);
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
