using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Geometry {
    public static class Transform3Methods {
        public static Plane XZPlane(this Transform3 transform) {
            return new Plane(transform.O, transform.Y);
        }
    }
}
