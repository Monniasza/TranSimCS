using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Geometry {
    public struct WorkingPlane {
        public Vector3 O;
        public Vector3 X;
        public Vector3 Y;

        public Vector2 Project(Vector3 p) {
            var d = p - O;
            var cx = Vector3.Dot(d, X);
            var dy = Vector3.Dot(d, Y);
            return new Vector2 (cx, dy);
        }
        public Vector3 Unproject(Vector2 p) {
            return O + X*p.X + Y*p.Y;
        }
        public Ray UnprojectRay(Vector2 p) {
            var normal = Vector3.Cross(Y, X);
            normal.Normalize();
            var pos = Unproject(p);
            return new Ray(pos, normal);
        }
    }
}
