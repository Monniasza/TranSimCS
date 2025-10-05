using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Roads;
using TranSimCS.Worlds;

namespace TranSimCS.Geometry {
    [StructLayout(LayoutKind.Explicit)]
    public struct RoadNodeReferenceFrame {
        [FieldOffset(0)]
        public Vector3 X;
        [FieldOffset(3)]
        public Vector3 Y;
        [FieldOffset(6)]
        public Vector3 Z;
        [FieldOffset(9)]
        public Vector3 O;
        [FieldOffset(12)]
        public float Width;
        [FieldOffset(0)]
        public Transform3 Transform;

        public RoadNodeReferenceFrame(Vector3 o, Vector3 x, Vector3 y, Vector3 z, float width) {
            O = o;
            X = x;
            Y = y;
            Z = z;
            Width = width;
        }
        public RoadNodeReferenceFrame(Transform3 transform, float width = 0) {
            Transform = transform;
            Width = width;
        }

        public static RoadNodeReferenceFrame Calculate(ObjPos position, float width = 0) {
            var transform = position.CalcReferenceFrame();
            return new RoadNodeReferenceFrame(transform, width);
        }
        public static RoadNodeReferenceFrame Calculate(ObjPos position, float width = 0, NodeEnd end = NodeEnd.Forward) {
            var transform = Calculate(position, width);
            if (end == NodeEnd.Backward) transform = transform.FromOtherSide();
            return transform;
        }

        public RoadNodeReferenceFrame FromOtherSide() {
            return new RoadNodeReferenceFrame(O + X * Width, -X, Y, -Z, Width);
        }

        public Transform3 FromRight() {
            var transform = Transform;
            transform.O += X * Width;
            return transform;
        }

        public readonly Vector3 TransformVector(Vector3 input) {
            return (input.X * X) + (input.Y * Y) + (input.Z * Z) + O;
        }
    }
}
