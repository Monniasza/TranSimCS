using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Geometry {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TransformQ : IEquatable<TransformQ>, IVertexType {
        public static TransformQ Identity => new(Vector3.Zero, Quaternion.Identity);

        static TransformQ() {
            Debug.Assert(Marshal.SizeOf<TransformQ>() == 28);
        }

        public VertexDeclaration VertexDeclaration => vertexDeclaration;
        public static VertexDeclaration vertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(12, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2)
        );

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

        public Matrix ToMatrix() =>
            Matrix.CreateFromQuaternion(Rotation)
            * Matrix.CreateTranslation(Position);

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
        public Ray Transform(Ray ray) {
            return new(Transform(ray.Position), Vector3.Transform(ray.Direction, Rotation));
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
    }
}
