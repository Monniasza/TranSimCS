using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using NLog.Targets;
using TranSimCS.Collections;
using TranSimCS.Model;
using TranSimCS.SilkNet;

namespace TranSimCS.Geometry {
    public struct Transform3 {
        public Vector3 X, Y, Z, O;
        public Transform3(Vector3 x, Vector3 y, Vector3 z, Vector3 o) {
            X = x;
            Y = y;
            Z = z;
            O = o;
        }
        public Transform3(Matrix4x4 mat) {
            X = new Vector3(mat.M11, mat.M12, mat.M13);
            Y = new Vector3(mat.M21, mat.M22, mat.M23);
            Z = new Vector3(mat.M31, mat.M32, mat.M33);
            O = new Vector3(mat.M41, mat.M42, mat.M43);
        }

        public readonly Vector3 Transform(Vector3 input) {
            return input.X * X + input.Y * Y + input.Z * Z + O;
        }
        public readonly Vertex Transform(Vertex input) {
            return new Vertex(Transform(input.Position), input.Color, input.TexCoord, input.Material, input.Emissive);
        }
        public readonly QuadOld Transform(QuadOld quad) {
            return new QuadOld(Transform(quad.a), Transform(quad.b), Transform(quad.c), Transform(quad.d));
        }

        public Transform3 Around() => new Transform3(-X, Y, -Z, O);
        public TransformQ ToQuaternion(){
            var matrix = ToMatrix(Vector3.Zero, X, Y, Z);
            var quaternion = Quaternion.CreateFromRotationMatrix(matrix);
            var transform = new TransformQ(O, quaternion);
            return transform;
        }

        public static Matrix4x4 ToMatrix(Vector3 pos, Vector3 lateral, Vector3 nrm, Vector3 tangent) {
            return new Matrix4x4(
                lateral.X, lateral.Y, lateral.Z, 0,
                nrm.X,     nrm.Y,     nrm.Z,     0,
                tangent.X, tangent.Y, tangent.Z, 0,
                pos.X,     pos.Y,     pos.Z,     1
            );
        }

        public static Vector3 ToYawPitchRoll(Vector3 lateral, Vector3 nrm, Vector3 tangent) {
            var pitch = MathF.Atan2(tangent.Y, GeometryUtils.hypot2(tangent.X, tangent.Z));
            var yaw = MathF.Atan2(tangent.X, tangent.Z);
            var y = Vector3.UnitY;
            var noTiltLateral = Vector3.Cross(y, tangent).Normalized();
            var yComp = lateral.Y;
            var xComp = Vector3.Dot(noTiltLateral, lateral);
            var roll = MathF.Atan2(yComp, xComp);
            return new Vector3(yaw, pitch, roll);
        }
        public void TransformInPlace(Mesh mesh) => mesh.Vertices.TransformInPlace(Transform);
        public void TransformInPlace(MultiMesh mesh) {
            foreach(var submesh in mesh.RenderBins) TransformInPlace(submesh.Value);
            var that = this;
            mesh.meshInstances.TransformInPlace(x => x.Transform(that.ToQuaternion()));
        }
    }
}