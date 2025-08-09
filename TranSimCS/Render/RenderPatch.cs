using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Render {
    public delegate VertexPositionColorTexture PointGenerator(Vector3 pointPos, Vector2 interpPos);

    public static class RenderPatch {
        public static void DrawDebugFence(IRenderBin mesh, ISpline<Vector3> spline, Vector3 height, Color color, int accuracy = 17) {
            var points = Geometry.GenerateSplinePoints(spline, accuracy);
            var strip = new VertexPositionColorTexture[accuracy * 2];
            for(int i = 0; i < accuracy; i++) {
                var pos = points[i];
                var pt1 = new VertexPositionColorTexture(pos, color, new(0, i));
                strip[2 * i] = pt1;
                var pt2 = new VertexPositionColorTexture(pos + height, color, new(1, i));
                strip[(2 * i) + 1] = pt2;
            }
            mesh.DrawStrip(strip);
        }

        /// <summary>
        /// C0D0 C1D0
        /// C0D1 C1D1
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="c0"></param>
        /// <param name="c1"></param>
        /// <param name="d0"></param>
        /// <param name="d1"></param>
        /// <param name="vertFn"></param>
        /// <param name="resC"></param>
        /// <param name="resD"></param>
        public static void RenderCoonsPatch(
            IRenderBin mesh,
            ISpline<Vector3> c0,
            ISpline<Vector3> c1,
            ISpline<Vector3> d0,
            ISpline<Vector3> d1,
            PointGenerator vertFn,
            int resC = 17,
            int resD = 17) {

            var lutC0 = Geometry.GenerateSplinePoints(c0, resC);
            var lutC1 = Geometry.GenerateSplinePoints(c1, resC);
            var lutD0 = Geometry.GenerateSplinePoints(d0, resD);
            var lutD1 = Geometry.GenerateSplinePoints(d1, resD);

            var results = new VertexPositionColorTexture[resC, resD];

            var cornerC0D0 = lutC0[0];
            var cornerC0D1 = lutC0[resC - 1];
            var cornerC1D0 = lutC1[0];
            var cornerC1D1 = lutC1[resC - 1];

            var stepS = 1.0f / (resC - 1);
            var stepT = 1.0f / (resD - 1);
            
            for (int s = 0; s < resC; s++) {
                for (int t = 0; t < resD; t++) {
                    var ns = resC - s - 1;
                    var nt = resD - t - 1;
                    var S = s * stepS;
                    var T = t * stepT;
                    var nS = 1 - S;
                    var nT = 1 - T;
                    var Lc = Vector3.Lerp(lutC0[s], lutC1[s], T);
                    var Ld = Vector3.Lerp(lutD0[t], lutD1[t], S);
                    var B = nS * nT * cornerC0D0
                           + S * nT * cornerC0D1
                          + nS *  T * cornerC1D0
                           + S *  T * cornerC1D1;
                    var pt = Lc + Ld - B;
                    var vertex = vertFn(pt, new(S, T));
                    results[s, t] = vertex;
                }
            }
            mesh.DrawGrid(results);
        }
        public static void L(float s, float t, ISpline<Vector3> c0, ISpline<Vector3> c1) => Vector3.Lerp(c0[s], c1[s], t);
    }
}
