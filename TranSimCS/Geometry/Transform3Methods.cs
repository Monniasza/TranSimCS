using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LanguageExt.ClassInstances;
using MonoGame.Extended;
using TranSimCS.Worlds;

namespace TranSimCS.Geometry {
    public static class Transform3Methods {
        public static Plane XZPlane(this Transform3 transform) => GeometryUtils.PointAndNormal(transform.O, transform.Y);

        public static TransformQ ToTransformQ(this PositionEulerAngles transform) {
            return new TransformQ(
                transform.Position,
                Quaternion.CreateFromYawPitchRoll(
                    GeometryUtils.FieldToRadians(transform.Azimuth),
                    -transform.Inclination,
                    transform.Tilt));
        }
        public static TransformQ ToTransformQ(this Matrix4x4 m) {
            Matrix4x4.Decompose(m,
                out _,
                out Quaternion rotation,
                out Vector3 position);
            return new TransformQ(position, rotation);
        }
        public static PositionEulerAngles ToObjPos(this TransformQ transform) {
            Matrix4x4 m = Matrix4x4.CreateFromQuaternion(transform.Rotation);
            

            Vector3 lateral = new(m.M11, m.M12, m.M13);
            Vector3 normal = new(m.M21, m.M22, m.M23);
            Vector3 tangent = new(m.M31, m.M32, m.M33);

            Vector3 ypr = Transform3.ToYawPitchRoll(
                lateral,
                normal,
                tangent);

            return new PositionEulerAngles(
                transform.Position,
                GeometryUtils.RadiansToField(ypr.X),
                ypr.Y,
                ypr.Z);
        }
    }
}
