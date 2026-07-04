using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Geometry {
    public struct TransformQ {
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
    }
}
