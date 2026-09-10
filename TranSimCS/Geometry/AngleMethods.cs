using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Geometry {
    public static class AngleMethods {
        public const float RadsInDeg = MathF.PI / 180;
        public const float DegsInRad = 180 / MathF.PI;
        public static Vector3 ToDegrees(this Vector3 vector) => vector * DegsInRad;
        public static Vector3 ToRadians(this Vector3 vector) => vector * RadsInDeg;

        public static float ToDegrees(this float vector) => vector * DegsInRad;
    }
}
