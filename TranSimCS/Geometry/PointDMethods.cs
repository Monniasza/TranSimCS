using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clipper2Lib;
using Microsoft.Xna.Framework;

namespace TranSimCS.Geometry {
    public static class PointDMethods {
        /// <summary>
        /// Converts a <see cref="PointD"/> to a <see cref="Vector2"/>
        /// </summary>
        /// <param name="point">source point</param>
        /// <returns>converted point</returns>
        public static Vector2 ToVector2(this PointD point) => new((float)point.x, (float)point.y);
    }
}
