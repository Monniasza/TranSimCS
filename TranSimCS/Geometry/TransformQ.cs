using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TranSimCS.Geometry {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TransformQ : IEquatable<TransformQ>{
        public static TransformQ Identity => new(Vector3.Zero, Quaternion.Identity);

        static TransformQ() {
            Debug.Assert(Marshal.SizeOf<TransformQ>() == 28);
        }
        public Vector3 Position;
        public Quaternion Rotation;

        public TransformQ(Vector3 position, Quaternion rotation) {
            Position = position;
            Rotation = Quaternion.Normalize(rotation);
        }

        public TransformQ Inverse() {
            Quaternion inverseRotation = Quaternion.Conjugate(Rotation);

            return new TransformQ(
                -Vector3.Transform(Position, inverseRotation),
                inverseRotation);
        }

        public Matrix4x4 ToMatrix() =>
            Matrix4x4.CreateFromQuaternion(Rotation)
            * Matrix4x4.CreateTranslation(Position);

        public TransformQ Append(TransformQ transform, Vector3 pivot) {
            TransformQ result = default;

            // Rotate position around pivot
            var relative = Position - pivot;
            relative = Vector3.Transform(relative, transform.Rotation);
            result.Position = pivot + relative + transform.Position;

            // Compose orientations
            result.Rotation = Quaternion.Normalize(
                transform.Rotation * Rotation);

            return result;
        }

        public Vector3 Transform(Vector3 vector) {
            return Position + Vector3.Transform(vector, Rotation);
        }
        public Ray3 Transform(Ray3 ray) {
            return new(Transform(ray.Origin), Vector3.Transform(ray.Direction, Rotation));
        }

        public override bool Equals(object? obj) {
            return obj is TransformQ q && Equals(q);
        }

        public bool Equals(TransformQ other) {
            return Position.Equals(other.Position) &&
                   Rotation.Equals(other.Rotation);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Position, Rotation);
        }

        /// <summary>
        /// Combines the transforms so <paramref name="b"/> is applied first, then <paramref name="a"/>
        /// </summary>
        /// <param name="a">The second transform</param>
        /// <param name="b">The first transform</param>
        /// <returns>a composition of <paramref name="a"/> and <paramref name="b"/></returns>
        public static TransformQ operator *(TransformQ a, TransformQ b) {
            return new TransformQ(
                a.Position + Vector3.Transform(b.Position, a.Rotation),
                Quaternion.Normalize(a.Rotation * b.Rotation));
        }

        public static bool operator ==(TransformQ left, TransformQ right) {
            return left.Equals(right);
        }

        public static bool operator !=(TransformQ left, TransformQ right) {
            return !(left == right);
        }

        public static TransformQ LookTowards(Vector3 origin, Vector3 cameraPos, Vector3 up) {
            Vector3 tangent = Vector3.Normalize(cameraPos - origin);
            Matrix4x4 mat = Matrix4x4.CreateWorld(origin, tangent, up);
            return new TransformQ(origin, mat.ToTransformQ().Rotation);
        }
    }
}
