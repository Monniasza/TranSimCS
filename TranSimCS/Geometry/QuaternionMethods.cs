using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Geometry {
    public static class QuaternionMethods {
        public static Quaternion Normalized(this Quaternion q) => Quaternion.Normalize(q);
        public static Quaternion Chebyshev(Quaternion a, Quaternion b, float amount) {
            float cosTheta = Quaternion.Dot(a, b);
            if(cosTheta < 0) {
                cosTheta *= -1;
                a *= -1;
            }
            float theta2 = 2 * (1 - cosTheta);
            var coeff2 = 0.083333333333333f;
            var coeff3 = 0.01875f;
            float theta = theta2 * (1 + theta2 * (coeff2 + theta2 * coeff3));
            var ratioA = amount * (1 + (theta * theta / 6) * (1 - amount * amount));
            var ratioB = amount * (1 - (theta * theta / 6) * (2*amount - amount*amount));

            return a*ratioA + b*ratioB;
        }
    }
}
