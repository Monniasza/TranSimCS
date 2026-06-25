using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt.ClassInstances;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using TranSimCS.Worlds;

namespace TranSimCS.Geometry {
    public static class Transform3Methods {
        public static Plane XZPlane(this Transform3 transform) {
            return new Plane(transform.O, transform.Y);
        }

        public static TransformQ ToTransformQ(this ObjPos transform) {
            return new TransformQ(
                transform.Position,
                Quaternion.CreateFromYawPitchRoll(
                    GeometryUtils.FieldToRadians(transform.Azimuth),
                    -transform.Inclination,
                    transform.Tilt));
        }
        public static TransformQ ToTransformQ(this Matrix m) {
            m.Decompose(
                out _,
                out Quaternion rotation,
                out Vector3 position);
            return new TransformQ(position, rotation);
        }
        public static ObjPos ToObjPos(this TransformQ transform) {

            Matrix m = Matrix.CreateFromQuaternion(transform.Rotation);

            Vector3 lateral = m.Right;
            Vector3 normal = m.Up;
            Vector3 tangent = m.Forward;

            Vector3 ypr = Transform3.ToYawPitchRoll(
                lateral,
                normal,
                tangent);

            return new ObjPos(
                transform.Position,
                GeometryUtils.RadiansToField(ypr.X),
                -ypr.Y,
                ypr.Z);
        }
    }
}
