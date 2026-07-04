using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Geometry {
    public static class OBB {
        public static BoundingBox TransformBoundingBox(BoundingBox box, TransformQ transform) {
            Vector3 center = (box.Min + box.Max) * 0.5f;
            Vector3 extent = (box.Max - box.Min) * 0.5f;

            Quaternion q = transform.Rotation;

            float xx = q.X * q.X;
            float yy = q.Y * q.Y;
            float zz = q.Z * q.Z;
            float xy = q.X * q.Y;
            float xz = q.X * q.Z;
            float yz = q.Y * q.Z;
            float wx = q.W * q.X;
            float wy = q.W * q.Y;
            float wz = q.W * q.Z;

            float m11 = 1 - 2 * (yy + zz);
            float m12 = 2 * (xy - wz);
            float m13 = 2 * (xz + wy);

            float m21 = 2 * (xy + wz);
            float m22 = 1 - 2 * (xx + zz);
            float m23 = 2 * (yz - wx);

            float m31 = 2 * (xz - wy);
            float m32 = 2 * (yz + wx);
            float m33 = 1 - 2 * (xx + yy);

            Vector3 newCenter =
                Vector3.Transform(center, q) +
                transform.Position;

            Vector3 newExtent = new(
                MathF.Abs(m11) * extent.X + MathF.Abs(m21) * extent.Y + MathF.Abs(m31) * extent.Z,
                MathF.Abs(m12) * extent.X + MathF.Abs(m22) * extent.Y + MathF.Abs(m32) * extent.Z,
                MathF.Abs(m13) * extent.X + MathF.Abs(m23) * extent.Y + MathF.Abs(m33) * extent.Z);

            return new BoundingBox(
                newCenter - newExtent,
                newCenter + newExtent);
        }
    }
}
