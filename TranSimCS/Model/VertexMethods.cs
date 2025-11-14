using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Model {
    public static class VertexMethods {
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VertexPositionColorTexture Add(this VertexPositionColor vpc, Vector2 uv) => new(vpc.Position, vpc.Color, uv);
    }
}
