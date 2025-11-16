

using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NLog.Targets;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.ModelOld;

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
        public readonly QuadOld Transform(QuadOld quad) {
            return new QuadOld(Transform(quad.a), Transform(quad.b), Transform(quad.c), Transform(quad.d));
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
        public void TransformOutOfPlace(Mesh src, Mesh dst) {
            if (dst == null) dst = src;
            var count = src.Vertices.Count;
            var transformedVertices = src.Vertices.Select(Transform).ToArray();
            dst.DrawModel(transformedVertices, src.Indices, src.Tags);
        }
        public void TransformOutOfPlace(MultiMesh src, MultiMesh dst) {
            foreach (var bin in src.RenderBins) {
                var tgtBin = dst.GetOrCreateRenderBinForced(bin.Key);
                TransformOutOfPlace(bin.Value, tgtBin);
            }
        }
        public void TransformInPlace(Mesh mesh) => mesh.Vertices.TransformInPlace(Transform);
        public void TransformInPlace(MultiMesh mesh) {
            foreach(var submesh in mesh.RenderBins) TransformInPlace(submesh.Value);
        }
    }
}