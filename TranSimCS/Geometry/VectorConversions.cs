using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EarClipperLib;
using Microsoft.Xna.Framework;
using PeterO.Numbers;

namespace TranSimCS.Geometry {
    public class VectorConversions {
        public static Vector3m ToRational(Vector3 vector) {
            return new Vector3m(ToRational(vector.X), ToRational(vector.Y), ToRational(vector.Z));
        }
        public static ERational ToRational(double number) {
            return ERational.FromEFloat(EFloat.FromDouble(number));
        }
        public static Vector3 FromRational(Vector3m vector) {
            return new(FromRational(vector.X), FromRational(vector.Y), FromRational(vector.Z));
        }
        public static float FromRational(ERational rational) {
            return (float)rational.Numerator.ToInt64Unchecked() / rational.Denominator.ToInt64Unchecked();
        }
    }
}
