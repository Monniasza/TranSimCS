

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS {
    public struct Transform3 {
        public Vector3 X, Y, Z, O;
        public Transform3(Vector3 x, Vector3 y, Vector3 z, Vector3 o) {
            X = x;
            Y = y;
            Z = z;
            O = o;
        }
        public Transform3(Matrix mat) {
            X = mat.Right;
            Y = mat.Up;
            Z = mat.Backward;
            O = mat.Translation;
        }

        public readonly Vector3 Transform(Vector3 input) {
            return (input.X * X) + (input.Y * Y) + (input.Z * Z) + O;
        }
        public readonly VertexPositionColorTexture Transform(VertexPositionColorTexture input) {
            return new VertexPositionColorTexture(Transform(input.Position), input.Color, input.TextureCoordinate);
        }
        public readonly Quad Transform(Quad quad) {
            return new Quad(Transform(quad.a), Transform(quad.b), Transform(quad.c), Transform(quad.d));
        }

        public Transform3 Around() => new Transform3(-X, Y, -Z, O);
    }
}