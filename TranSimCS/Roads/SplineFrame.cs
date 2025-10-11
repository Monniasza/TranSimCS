using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        public Vector3 UnTransform(Vector3 position, float minT = 0, float maxT = 1, int depth = 24, float tolerance = 1e-3f) {
            Vector3 pO, vX, vY;

            //Describe the solution as finding a plane that intersects a point, then find X and Y.
            float candidatePos = 0;
            for (int i = 0; i < depth; i++) {
                var midpoint = (minT + maxT) / 2;
                pO = CenterSpline[midpoint];
                vX = XPlusSpline[midpoint];
                vY = YPlusSpline[midpoint];
                var tangential = -Vector3.Cross(vY, vX);
                var dist = SignedDistance(pO, tangential, position);
                if (MathF.Abs(dist) < tolerance) {
                    //Satisfactory tolerance
                    candidatePos = midpoint;
                    break;
                }
                if(dist > 0) {
                    //Increase T
                    minT = midpoint;
                } else {
                    //Decrease T
                    maxT = midpoint;
                }
            }

            pO = CenterSpline[candidatePos];
            vX = XPlusSpline[candidatePos];
            vX.Normalize();
            vY = YPlusSpline[candidatePos];
            vY.Normalize();
            var d = position - pO;
            var x = Vector3.Dot(d, vX);
            var y = Vector3.Dot(d, vY);
            return new Vector3(x, y, candidatePos);
        }
        public Vector3 Transform(Vector3 input) {
            var pO = CenterSpline[input.Z];
            var pX = XPlusSpline[input.Z];
            var pY = YPlusSpline[input.Z];
            return pO + pX * input.X + pY * input.Y;
        }


        public static float SignedDistance(Vector3 position, Vector3 normal, Vector3 subject) {
            normal.Normalize();
            return Vector3.Dot((subject - position), normal);
        }
    }
}
