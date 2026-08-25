using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.SilkNet {
    [StructLayout(LayoutKind.Sequential)]
    public struct ShaderUniformData {
        public Matrix4x4 WorldViewProjection;
        public Vector4 AmbientColor;
        public float AlphaCutoff;
        public float EmissiveIsMask;
        public float Reserved0;
        public float Reserved1;
    }
}
