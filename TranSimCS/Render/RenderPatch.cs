using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Spline;

namespace TranSimCS.Render {
    public delegate VertexPositionColorTexture PointGenerator(Vector3 pointPos, Vector2 interpPos);

    public static class RenderPatch {
        public static void DrawDebugFence(Mesh mesh, ISpline<Vector3> spline, Vector3 height, Color color, int accuracy = 17) {
            var points = GeometryUtils.GenerateSplinePoints(spline, accuracy);
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
        /// Generates a Coons patch, a square-like patch made with four border splines with four conditions:
        /// <br/> C0[0] = D0[0]
        /// <br/> C0[1] = D1[0]
        /// <br/> C1[0] = D0[1]
        /// <br/> C1[1] = D1[1]
        /// </summary>
        /// <param name="mesh">render bin to render to</param>
        /// <param name="c0">top patch, going right</param>
        /// <param name="c1">bottom patch, going right</param>
        /// <param name="d0">left patch, going down</param>
        /// <param name="d1">right patch, going down</param>
        /// <param name="vertFn">converts positions and UVs into vertices</param>
        /// <param name="resC">resolution of the top and bottom</param>
        /// <param name="resD">resolution of the left and right</param>
        public static void RenderCoonsPatch(
            Mesh mesh,
            ISpline<Vector3> c0,
            ISpline<Vector3> c1,
            ISpline<Vector3> d0,
            ISpline<Vector3> d1,
            PointGenerator vertFn,
            int resC = 17,
            int resD = 17) {

            var lutC0 = GeometryUtils.GenerateSplinePoints(c0, resC);
            var lutC1 = GeometryUtils.GenerateSplinePoints(c1, resC);
            var lutD0 = GeometryUtils.GenerateSplinePoints(d0, resD);
            var lutD1 = GeometryUtils.GenerateSplinePoints(d1, resD);

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
