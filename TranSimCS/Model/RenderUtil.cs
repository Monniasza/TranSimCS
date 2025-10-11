using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Geometry;

namespace TranSimCS.Model {
    /// <summary>
    /// Various algorithms for meshes
    /// </summary>
    public static class RenderUtil {
        //Rendering methods for different shapes and primitives
        /// <summary>
        /// Draws a quad using the specified vertices. They must be in the clockwise order to form a quad.
        /// </summary>
        /// <param name="a">first vertex</param>
        /// <param name="b">second verted</param>
        /// <param name="c">third vertex</param>
        /// <param name="d">fourth vertex</param>
        private static readonly int[] indexDataQuadLookup = [0, 1, 2, 0, 2, 3];
        public static void DrawQuad(this IRenderBin rb, VertexPositionColorTexture a, VertexPositionColorTexture b, VertexPositionColorTexture c, VertexPositionColorTexture d) {
            int indexA = rb.AddVertex(a);
            int indexB = rb.AddVertex(b);
            int indexC = rb.AddVertex(c);
            int indexD = rb.AddVertex(d);
            int[] indexDataQuad = [indexA, indexB, indexC, indexD];
            foreach (var index in indexDataQuadLookup) {
                rb.AddIndex(indexDataQuad[index]);
            }
        }
        public static void DrawQuad(this IRenderBin rb, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color) {
            rb.DrawQuad(
                new VertexPositionColorTexture(a, color, new(0, 0)),
                new VertexPositionColorTexture(b, color, new(1, 0)),
                new VertexPositionColorTexture(c, color, new(1, 1)),
                new VertexPositionColorTexture(d, color, new(0, 1)));
        }
        public static void DrawQuad(this IRenderBin rb, Quad q) => rb.DrawQuad(q.a, q.b, q.c, q.d);
        public static void DrawQuad(this IRenderBin rb, int a, int b, int c, int d) {
            int[] indexDataQuad = [a, b, c, d];
            foreach (var index in indexDataQuadLookup) rb.AddIndex(indexDataQuad[index]);
        }

        /// <summary>
        /// Draws a triangle using the specified vertices. They must be in the clockwise order to form a triangle.
        /// </summary>
        /// <param name="a">first vertex</param>
        /// <param name="b">second vertex</param>
        /// <param name="c">third vertex</param>
        public static void DrawTriangle(this IRenderBin rb, VertexPositionColorTexture a, VertexPositionColorTexture b, VertexPositionColorTexture c) {
            int indexA = rb.AddVertex(a);
            int indexB = rb.AddVertex(b);
            int indexC = rb.AddVertex(c);
            rb.DrawTriangle(indexA, indexB, indexC);
        }

        /// <summary>
        /// Draws a strip of vertices, where each vertex is connected to the next one in a triangle strip fashion.
        /// The vertices should start at the bottom left and even positions are the left vertices, odd positions are the right vertices.
        /// </summary>
        /// <param name="vertices"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void DrawStrip(this IRenderBin rb, VertexPositionColorTexture[] vertices) {
            ArgumentNullException.ThrowIfNull(vertices);
            if (vertices.Length < 3) throw new ArgumentException("At least three vertices are required to draw a strip.");
            int[] newVertexIds = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                newVertexIds[i] = rb.AddVertex(vertices[i]);
            for (int i = 0; i < newVertexIds.Length - 2; i++) {
                rb.AddIndex(newVertexIds[i]);
                if ((i & 1) == 0) {
                    rb.AddIndex(newVertexIds[i + 2]);
                    rb.AddIndex(newVertexIds[i + 1]);
                } else {
                    rb.AddIndex(newVertexIds[i + 1]);
                    rb.AddIndex(newVertexIds[i + 2]);
                }

            }
        }

        public static void DrawLine(this IRenderBin rb, Vector3 start, Vector3 end, Vector3 normal, Color c, float width = 0.2f) {
            var len = end - start;
            var cross = Vector3.Cross(normal, len);
            cross.Normalize();
            cross *= width / 2;

            var p1 = end - cross;
            var p2 = end + cross;
            var p3 = start + cross;
            var p4 = start - cross;
            rb.DrawQuad(
                new VertexPositionColorTexture(p1, c, new(0, 0)),
                new VertexPositionColorTexture(p2, c, new(1, 0)),
                new VertexPositionColorTexture(p3, c, new(1, 1)),
                new VertexPositionColorTexture(p4, c, new(0, 1))
           );
        }

        public static void DrawClosedStrip(this IRenderBin rb, VertexPositionColorTexture[] l, VertexPositionColorTexture[] r) {
            var woven = GeometryUtils.WeaveStrip(l, r).ToList();
            woven.Add(l[0]);
            woven.Add(r[0]);
            DrawStrip(rb, woven.ToArray());
        }

        public static void DrawStrip(this IRenderBin rb, VertexPositionColorTexture[] l, VertexPositionColorTexture[] r)
            => DrawStrip(rb, GeometryUtils.WeaveStrip(l, r));

        public static void DrawCenteredPoly(this IRenderBin rb, VertexPositionColorTexture center, params VertexPositionColorTexture[] perimeter) {
            var centerIdx = rb.AddVertex(center);
            var perimeterIndexes = new int[perimeter.Length];
            for (int i = 0; i < perimeter.Length; i++)
                perimeterIndexes[i] = rb.AddVertex(perimeter[i]);
            for (int i = 0; i < perimeter.Length; i++) {
                var idx1 = perimeterIndexes[i];
                var idx2 = perimeterIndexes[(i + 1) % perimeterIndexes.Length];
                var idx0 = centerIdx;
                rb.DrawTriangle(idx0, idx1, idx2);
            }
        }

        /// <summary>
        /// Draws a grid of vertices, where each square is made up of two triangles.
        /// The vertices should be in a 2D array, where each element is a VertexPositionColorTexture.
        /// The Elements should be in the order of (x, y), where x is the horizontal index and y is the vertical index.
        /// The elements should start at top left
        /// </summary>
        /// <param name="vertices"></param>
        public static void DrawGrid(this IRenderBin rb, VertexPositionColorTexture[,] vertices) {
            ArgumentNullException.ThrowIfNull(vertices);
            int height = vertices.GetLength(1);
            int width = vertices.GetLength(0);
            int[,] newVertexIds = new int[width, height];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    newVertexIds[i, j] = rb.AddVertex(vertices[i, j]);
            for (int i = 0; i < width - 1; i++) {
                for (int j = 0; j < height - 1; j++) {
                    // Draw two triangles for each square in the grid
                    rb.AddIndex(newVertexIds[i, j]);
                    rb.AddIndex(newVertexIds[i + 1, j]);
                    rb.AddIndex(newVertexIds[i, j + 1]);
                    rb.AddIndex(newVertexIds[i + 1, j + 1]);
                    rb.AddIndex(newVertexIds[i, j + 1]);
                    rb.AddIndex(newVertexIds[i + 1, j]);
                }
            }
        }
    }
}
