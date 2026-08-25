using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.SilkNet {
    public static class NumericsMethods {
        public static Matrix4x4 Transpose(this Matrix4x4 matrix) => Matrix4x4.Transpose(matrix);
    }
}
