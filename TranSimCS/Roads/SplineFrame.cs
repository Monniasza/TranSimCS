using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Spline;

namespace TranSimCS.Roads {
    public struct SplineFrame {
        public Bezier3 CenterSpline;
        public Bezier3 XPlusSpline;
        public Bezier3 YPlusSpline;
        public Bezier3 ZPlusSpline;

        public SplineFrame(Bezier3 centerSpline, Bezier3 xPlusSpline, Bezier3 yPlusSpline, Bezier3 zPlusSpline) {
            CenterSpline = centerSpline;
            XPlusSpline = xPlusSpline;
            YPlusSpline = yPlusSpline;
            ZPlusSpline = zPlusSpline;
        }

        public Bezier3 CreateFromStartEnd(Vector3 relStart, Vector3 relEnddd) {
            var A = CenterSpline.a + relStart.X * XPlusSpline.a + relStart.Y * YPlusSpline.a + relStart.Z * ZPlusSpline.a;
            var B = CenterSpline.b + relStart.X * XPlusSpline.b + relStart.Y * YPlusSpline.b + relStart.Z * ZPlusSpline.b;
            var C = CenterSpline.c + relEnddd.X * XPlusSpline.c + relEnddd.Y * YPlusSpline.c + relEnddd.Z * ZPlusSpline.c;
            var D = CenterSpline.d + relEnddd.X * XPlusSpline.d + relEnddd.Y * YPlusSpline.d + relEnddd.Z * ZPlusSpline.d;
            return new Bezier3 (A, B, C, D);
        }
        public Bezier3 CreateFromPosition(Vector3 position) => CreateFromStartEnd(position, position);
    }
}
