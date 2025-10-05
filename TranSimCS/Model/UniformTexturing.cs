using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Model {
    public delegate T VertexGen<T>(Vector3 vector, float distance, int index);

    public static class UniformTexturing {
        public static VertexGen<VertexPositionColorTexture> WithFixedU(float u, Color? color = null) {
            var color0 = color ?? Color.White;
            return (p, d, i) => new VertexPositionColorTexture(p, color0, new(u, d));
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
    }
}
