

using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NLog.Targets;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;

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
        public readonly Quad<VertexPositionColorTexture> Transform(Quad<VertexPositionColorTexture> quad) {
            return new Quad<VertexPositionColorTexture>(Transform(quad.A), Transform(quad.B), Transform(quad.C), Transform(quad.D));
        }

        public Transform3 Around() => new Transform3(-X, Y, -Z, O);

        public static Matrix ToMatrix(Vector3 pos, Vector3 lateral, Vector3 nrm, Vector3 tangent) {
            return new Matrix(new Vector4(lateral, 0), new Vector4(nrm, 0), new Vector4(tangent, 0), new Vector4(pos, 1));
        }

        public static Vector3 ToYawPitchRoll(Vector3 lateral, Vector3 nrm, Vector3 tangent) {
            var pitch = MathF.Atan2(tangent.Y, GeometryUtils.hypot2(tangent.X, tangent.Z));
            var yaw = MathF.Atan2(tangent.X, tangent.Z);
            var y = Vector3.UnitY;
            var noTiltLateral = Vector3.Cross(y, tangent);
            noTiltLateral.Normalize();
            var yComp = lateral.Y;
            var xComp = Vector3.Dot(noTiltLateral, lateral);
            var roll = MathF.Atan2(yComp, xComp);
            return new Vector3(yaw, pitch, roll);
        }
        public void TransformOutOfPlace(MeshElement<SimpleMaterial, VertexPositionColorTexture> src, MeshBuilder<SimpleMaterial, VertexPositionColorTexture> dst) {
            var count = src.Vertices.Length;
            var transformedVertices = src.Vertices.Select(Transform).ToArray();
            dst.AddAll(transformedVertices, src.Triangles);
        }
        public MeshElement<SimpleMaterial, VertexPositionColorTexture> Transform(MeshElement<SimpleMaterial, VertexPositionColorTexture> src) {
            var count = src.Vertices.Length;
            var transformedVertices = src.Vertices.Select(Transform).ToArray();
            return new(src.Name, src.Material, transformedVertices, src.Triangles, src.IsVisible, src.VertexProcessor);
        }
        public void TransformOutOfPlace(MeshComplex src, MeshComplex dst) {
            foreach (var bin in src.Elements) {
                if(bin is MeshElement<SimpleMaterial, VertexPositionColorTexture> bin0) {
                    dst.AddElement(Transform(bin0));
                }
            }
        }
        public void TransformInPlace(MeshElement<SimpleMaterial, VertexPositionColorTexture> mesh) => mesh.Vertices.TransformInPlace(Transform);
        public void TransformInPlace(MeshComplex mesh) {
            foreach(var submesh in mesh.Elements) {
                if(submesh is MeshElement<SimpleMaterial, VertexPositionColorTexture> bin0) TransformInPlace(bin0);
            }
        }
    }
}