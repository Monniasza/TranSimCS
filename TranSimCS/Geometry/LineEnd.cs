using System;
using System.Numerics;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;

namespace TranSimCS.Geometry
{
    [Obsolete("Removal planned")]
    public readonly struct LineEnd {
        public Vector3 Position { get; }
        public Vector3 Tangential { get; }
        public Vector3 Normal { get; }
        public Vector3 Lateral { get; }
        public Ray3 Ray => new Ray3(Position, Tangential);

        public LineEnd(Vector3 position, Vector3 tangential, Vector3 normal, Vector3 lateral) {
            Position = position;
            Tangential = tangential;
            Normal = normal;
            Lateral = lateral;
        }

        public static LineEnd calcLineEnd(HalfNode node, float offset)
            => calcLineEnd(node, offset, NodeEnd.Forward);
        public static LineEnd calcBoundingLineEndFaced(HalfNode node, int discriminator = 1) {
            var (l, r) = node.Bounds;
            if (discriminator < 0)
                return calcLineEnd(node, l);
            return calcLineEnd(node, r);
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
