using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Maths;
using TranSimCS.Geometry;

namespace TranSimCS.Model {
    /// <summary>
    /// Various algorithms for meshes
    /// </summary>
    public static class RenderUtil {
        public static void InvertNormals<T>(T[] indices) {
            for(int i = 0; i < indices.Length; i += 3) {
                (indices[i + 1], indices[i])
              = (indices[i], indices[i + 1]);
            }
        }
    }
}
