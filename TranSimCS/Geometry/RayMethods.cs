using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Geometry {
    public static class RayMethods {
        public static Ray Transform(Ray ray, Matrix mat) {
            var tr = mat.Translation;
            var start = Vector3.Transform(ray.Position, mat);
            var dir = Vector3.Transform(ray.Direction, mat) - tr;
            return new Ray(start, dir);
        }
    }
}
