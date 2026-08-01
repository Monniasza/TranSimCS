using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Geometry;

namespace TranSimCS.Model {
    public delegate T VertexGen<T>(Vector3 vector, float distance, int index);
    public delegate (T, T) VertexGen2<T>(Vector3 l, Vector3 r, float distanceL, float distanceR, int index);

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
        public static (T[], T[]) UniformTexturedTwin<T>(Vector3[] l, Vector3[] r, VertexGen2<T> vertexer, float bias = 0.5f, float leftSkew = 0, float rightSkew = 0) {
            ArgumentNullException.ThrowIfNull(l, nameof(l));
            ArgumentNullException.ThrowIfNull(r, nameof(r));
            if (l.Length != r.Length) throw new ArgumentException("Lengths are not equal");
            var count = l.Length;
            if (count == 0) return ([], []);

            //Generate cumulative lookup table
            var previousL = l[0];
            var previousR = r[0];
            Vector2[] cumulativeDistances = new Vector2[count];
            Vector2 cumulativeDistance = Vector2.Zero;
            for(int i = 0; i < count; i++) {
                var left = l[i];
                var right = r[i];
                var leftLength = Vector3.Distance(previousL, left);
                var rightLength = Vector3.Distance(previousR, right);
                cumulativeDistance += new Vector2(leftLength, rightLength);
                cumulativeDistances[i] = cumulativeDistance;
                previousL = left;
                previousR = right;
            }

            //Check validity
            var degenerateLeft = cumulativeDistance.X < 0.001;
            var degenerateRight = cumulativeDistance.Y < 0.001;
            if (degenerateLeft) {
                Debug.Assert(!degenerateRight, "Both strips are dengenerated");
                cumulativeDistance.X = cumulativeDistance.Y;
                for (int i = 0; i < cumulativeDistances.Length; i++) cumulativeDistances[i] = new(cumulativeDistances[i].Y);
            } else if (degenerateRight) {
                cumulativeDistance.Y = cumulativeDistance.X;
                for (int i = 0; i < cumulativeDistances.Length; i++) cumulativeDistances[i] = new(cumulativeDistances[i].X);
            }

            //Compensate arc-lengths
            var arclength = cumulativeDistance.Lerp(bias);
            var arclengthCorrectiveMultipliers = new Vector2(arclength) / cumulativeDistance;
            var skewDistances = new Vector2(leftSkew, rightSkew);

            //Generate vertices
            T[] lverts = new T[l.Length];
            T[] rverts = new T[l.Length];
            for (int i = 0; i < l.Length; i++) {
                var currentDistance = cumulativeDistances[i] * arclengthCorrectiveMultipliers + skewDistances;
                var vertices = vertexer(l[i], r[i], currentDistance.X, currentDistance.Y, i);
                lverts[i] = vertices.Item1;
                rverts[i] = vertices.Item2;
            }
            return (lverts, rverts);
        }

        public static VertexGen2<VertexPositionColorTexture> GenerateLaneStripVertexGen(Color c) {
            (VertexPositionColorTexture, VertexPositionColorTexture) GenerateVertices(Vector3 l, Vector3 r, float distanceL, float distanceR, int index) {
                float mutualDistance = Vector3.Distance(l, r) / 2;
                return (
                    new VertexPositionColorTexture(l, c, new(-mutualDistance, distanceL)),
                    new VertexPositionColorTexture(r, c, new(mutualDistance, distanceR))
                );
            }
            return GenerateVertices;
        }
    }
}
