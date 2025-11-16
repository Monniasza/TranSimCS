using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Model {
    public delegate T VertexGen<T>(Vector3 vector, float distance, int index);
    public delegate (T, T) VertexGen2<T>(Vector3 l, Vector3 r, float distance, int index);

    public static class UniformTexturing {
        public static VertexGen<VertexPositionColorTexture> WithFixedU(float u, Color? color = null) {
            var color0 = color ?? Color.White;
            return (p, d, i) => new VertexPositionColorTexture(p, color0, new(u, d));
        }
        public static VertexGen2<VertexPositionColorTexture> PairStrip(float l = 0, float r = 1, Color? color = null) {
            return FromTwo<VertexPositionColorTexture>(WithFixedU(l, color), WithFixedU(r, color));
        }
        public static VertexGen2<T> FromTwo<T>(VertexGen<T> l, VertexGen<T> r) {
            return (ll, rr, d, i) => (l(ll, d, i), r(rr, d, i));
        }

        public static T[] UniformTextured<T>(Vector3[] vectors, VertexGen<T> vertexer) {
            T[] verts = new T[vectors.Length];

            verts[0] = vertexer(vectors[0], 0, 0);

            float distance = 0;
            for (int i = 1; i < vectors.Length; i++) {
                var prev = vectors[i - 1];
                var next = vectors[i];
                var dDistance = Vector3.Distance(prev, next);
                distance += dDistance;
                var vert = vertexer(next, distance, 0);
                verts[i] = vert;
            }
            return verts;
        }
        public static (T[], T[]) UniformTexturedTwin<T>(Vector3[] l, Vector3[] r, VertexGen2<T> vertexer) {
            T[] lverts = new T[l.Length];
            T[] rverts = new T[l.Length];

            var vert = vertexer(l[0], r[0], 0, 0);
            lverts[0] = vert.Item1;
            rverts[0] = vert.Item2;

            float distance = 0;
            for (int i = 1; i < l.Length; i++) {
                var prevl = l[i - 1];
                var nextl = l[i];
                var prevr = r[i - 1];
                var nextr = r[i];
                var dDistance = (Vector3.Distance(prevl, nextl) + Vector3.Distance(prevr, nextr)) / 2;
                distance += dDistance;
                vert = vertexer(nextl, nextr, distance, 0);
                lverts[i] = vert.Item1;
                rverts[i] = vert.Item2;
            }
            return (lverts, rverts);
        }
    }
}
