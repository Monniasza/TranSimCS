using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clipper2Lib;
using Microsoft.Xna.Framework;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Geometry {
    public static class VectorMethods {
        public static void CheckSpline(Bezier3 bezier, string name = "") {
            CheckVector(bezier.a, name + ".a"); CheckVector(bezier.b, name + ".b");
            CheckVector(bezier.c, name + ".c"); CheckVector(bezier.d, name + ".d");
        }

        public static void CheckVector(Vector3 vector, string name = "") {
            if (!float.IsFinite(vector.X)) throw new ArgumentException(name + ".X === NaN");
            if (!float.IsFinite(vector.Y)) throw new ArgumentException(name + ".Y === NaN");
            if (!float.IsFinite(vector.Z)) throw new ArgumentException(name + ".Z === NaN");
        }

        public static void CheckPosition(PositionEulerAngles vector, string name = "") {
            if (!float.IsFinite(vector.Position.X)) throw new ArgumentException(name + ".X === NaN");
            if (!float.IsFinite(vector.Position.Y)) throw new ArgumentException(name + ".Y === NaN");
            if (!float.IsFinite(vector.Position.Z)) throw new ArgumentException(name + ".Z === NaN");
            if (!float.IsFinite(vector.Inclination)) throw new ArgumentException(name + ".Inclination === NaN");
            if (!float.IsFinite(vector.Tilt)) throw new ArgumentException(name + ".Tilt === NaN");
        }

        public static bool IsFinite(this Vector3 vector) => float.IsFinite(vector.X) && float.IsFinite(vector.Y) && float.IsFinite(vector.Z);

        public static PointD ToPointD(this Vector2 vector) {
            return new PointD((float)vector.X, (float)vector.Y);
        }

        public static Vector3 ToX0Z(this Vector3 vector) {
            return new(vector.X, 0, vector.Z);
        }

        public static Vector3 ToXYZ(this Vector4 vector) =>
            new Vector3(vector.X, vector.Y, vector.Z);
    }
}
