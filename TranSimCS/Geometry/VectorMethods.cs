using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clipper2Lib;
using Microsoft.Xna.Framework;

namespace TranSimCS.Geometry {
    public static class VectorMethods {
        public static void CheckVector(Vector3 vector, string name = "") {
            if (float.IsNaN(vector.X)) throw new ArgumentException(name + ".X === NaN");
            if (float.IsNaN(vector.Y)) throw new ArgumentException(name + ".Y === NaN");
            if (float.IsNaN(vector.Z)) throw new ArgumentException(name + ".Z === NaN");
        }

        public static PointD ToPointD(this Vector2 vector) {
            return new PointD((float)vector.X, (float)vector.Y);
        }
    }
}
