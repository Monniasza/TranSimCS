using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Maths;
using TranSimCS.Geometry;

namespace TranSimCS.Model {
    /// <summary>
    /// Methods for <see cref="MeshBuilder{TMaterial, TVertex}"/>
    /// </summary>
    public static class MeshBuilderMethods {
        /// <summary>
        /// Add all vertices and triangles from a different model
        /// </summary>
        /// <typeparam name="TMaterial">type of materials</typeparam>
        /// <typeparam name="TVertex">type of vertices</typeparam>
        /// <param name="meshBuilder">target mesh builder</param>
        /// <param name="verts">vertices to add</param>
        /// <param name="triangles">triangles to add</param>
        /// <returns>meshBuilder</returns>
        public static MeshBuilder<TMaterial, TVertex> AddAll<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> meshBuilder, IEnumerable<TVertex> verts, IEnumerable<MeshTri> triangles) {
            var offset = meshBuilder.Verts.Count;
            meshBuilder.Verts.AddRange(verts);
            meshBuilder.Tris.AddRange(triangles.Select(x => x + offset));
            return meshBuilder;
        }
        /// <summary>
        /// Add all vertices and triangles from a different model
        /// </summary>
        /// <typeparam name="TMaterial">type of materials</typeparam>
        /// <typeparam name="TVertex">type of vertices</typeparam>
        /// <param name="meshBuilder">target mesh builder</param>
        /// <param name="other">source mesh builder</param>
        /// <returns>meshBuilder</returns>
        public static MeshBuilder<TMaterial, TVertex> AddAll<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> meshBuilder, MeshBuilder<TMaterial, TVertex> other)
            => AddAll(meshBuilder, other.Verts, other.Tris);
        /// <summary>
        /// Add all vertices and triangles from a different model
        /// </summary>
        /// <typeparam name="TMaterial">type of materials</typeparam>
        /// <typeparam name="TVertex">type of vertices</typeparam>
        /// <param name="meshBuilder">target mesh builder</param>
        /// <param name="other">source mesh element</param>
        /// <returns>meshBuilder</returns>
        public static MeshBuilder<TMaterial, TVertex> AddAll<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> meshBuilder, MeshElement<TMaterial, TVertex> other)
            => AddAll(meshBuilder, other.Vertices, other.Triangles);

        //QUADS
        public static MeshBuilder<TMaterial, TVertex> DrawQuad<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> meshBuilder, IEnumerable<TVertex> quad, object? tag = null)
            => AddAll(meshBuilder, quad, [new(0, 1, 2, tag), new(0, 2, 3, tag)]);
        public static MeshBuilder<TMaterial, TVertex> DrawQuad<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> meshBuilder, Quad<TVertex> quad)
            => DrawQuad(meshBuilder, quad, quad.Tag);
        public static MeshBuilder<TMaterial, TVertex> DrawQuadIndices<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> meshBuilder, int[] quad, object? tag = null)
            => DrawQuadIndices(meshBuilder, quad[0], quad[1], quad[2], quad[3], tag);
        public static MeshBuilder<TMaterial, TVertex> DrawQuadIndices<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> meshBuilder, int a, int b, int c, int d, object? tag = null) {
            meshBuilder.Tris.AddRange([new(a, b, c, tag), new(a, c, d, tag)]);
            return meshBuilder;
        }

        //GRIDS
        public static MeshBuilder<TMaterial, TVertex> DrawGrid<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> meshBuilder, TVertex[,] verts, object? tag = null) {
            var width = verts.GetLength(0);
            var height = verts.GetLength(1);

            var quads = new Quad<int>[width-1 * height-1];

            for(int x = 1, i = 0; x < width; x++) {
                for (int y = 1; y < height; y++, i++) {
                    var j = x + y * width;
                    var q = new Quad<int>(j - 1 - width, j - 1, j, j - width, tag);
                    quads[i] = q;
                }
            }
            return meshBuilder;
        }

        //QUADS (POS/COL?/AUTO UV)
        public static MeshBuilder<TMaterial, VertexPositionColorTexture> DrawQuadUV<TMaterial>(this MeshBuilder<TMaterial, VertexPositionColorTexture> meshBuilder, Quad<VertexPositionColor> quad) {
            return DrawQuad(meshBuilder, [
                quad.A.Add(new(0, 0)), quad.B.Add(new(1, 0)),
                quad.C.Add(new(1, 1)), quad.D.Add(new(0, 1))
            ], quad.Tag);
        }
        public static MeshBuilder<TMaterial, VertexPositionColorTexture> DrawQuadUV<TMaterial>(this MeshBuilder<TMaterial, VertexPositionColorTexture> meshBuilder, Color c, Quad<Vector3> quad) {
            return DrawQuad(meshBuilder, [
                new VertexPositionColorTexture(quad.A, c, new(0, 0)),
                new VertexPositionColorTexture(quad.B, c, new(1, 0)),
                new VertexPositionColorTexture(quad.C, c, new(1, 1)),
                new VertexPositionColorTexture(quad.D, c, new(0, 1))
            ], quad.Tag);
        }
        public static MeshBuilder<TMaterial, VertexPositionColorTexture> DrawQuadUV<TMaterial>(this MeshBuilder<TMaterial, VertexPositionColorTexture> meshBuilder, Color color, Vector3 a, Vector3 b, Vector3 c, Vector3 d, object? tag = null, RectangleF? rect = null) {
            var l = rect?.Left ?? 0;
            var u = rect?.Top ?? 1;
            var r = rect?.Right ?? 1;
            var D = rect?.Bottom ?? 0;
            return DrawQuad(meshBuilder, [
                new VertexPositionColorTexture(a, color, new(l, D)),
                new VertexPositionColorTexture(b, color, new(r, D)),
                new VertexPositionColorTexture(c, color, new(r, u)),
                new VertexPositionColorTexture(d, color, new(l, u))
            ], tag);
        }

        //PARALLELOGRAMS (POS/COL/UV?)
        public static MeshBuilder<TMaterial, VertexPositionColorTexture> DrawParallelogram<TMaterial>(this MeshBuilder<TMaterial, VertexPositionColorTexture> meshBuilder, Vector3 a, Vector3 x, Vector3 y, Color c, object? tag = null, RectangleF ? rect = null) {
            var p1 = a;
            var p2 = a + x;
            var p3 = p2 + y;
            var p4 = a + y;
            return DrawQuadUV(meshBuilder, c, p1, p2, p3, p4, tag, rect);
        }

        //LINES (POS/COL/UV)
        public static MeshBuilder<TMaterial, VertexPositionColorTexture> DrawLine<TMaterial>(this MeshBuilder<TMaterial, VertexPositionColorTexture> meshBuilder, Vector3 start, Vector3 end, Vector3 normal, Color c, float width = 0.2f) {
            var len = end - start;
            var cross = Vector3.Cross(normal, len);
            cross.Normalize();
            cross *= width / 2;
            var p1 = end - cross;
            var p2 = end + cross;
            var p3 = start + cross;
            var p4 = start - cross;
            return DrawQuadUV(meshBuilder, c, p1, p2, p3, p4);
        }

        //TAGS
        public static MeshBuilder<TMaterial, TVertex> TagAll<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> meshBuilder, object? tag = null) {
            for (int i = 0; i < meshBuilder.Tris.Count; i++) {
                var t = meshBuilder.Tris[i];
                t.Tag = tag;
                meshBuilder.Tris[i] = t;
            }
            return meshBuilder;
        }
        public static void TagLast(this MeshElement rb, int count, object value) {
            if (value == null) return;
            if (count == 0) return;
            int startIndex = (rb.Triangles.Length) - count; // Each triangle has 3 indices
            if (count < 0) {
                startIndex = 0;
                count = rb.Triangles.Length;
            }
            for (int i = 0; i < count; i++) {
                var tri = rb.Triangles[i];
                tri.Tag = value;
                rb.Triangles[i] = tri;
            }
        }

        //STRIPS
        /// <summary>
        /// Draws a strip of vertices, where each vertex is connected to the next one in a triangle strip fashion.
        /// The vertices should start at the bottom left and even positions are the left vertices, odd positions are the right vertices.
        /// </summary>
        /// <param name="vertices"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void DrawStrip<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> rb, TVertex[] vertices, object? tag = null) {
            ArgumentNullException.ThrowIfNull(vertices, nameof(vertices));
            if (vertices.Length < 3) throw new ArgumentException("At least three vertices are required to draw a strip.");
            int baseIDX = rb.AddVertices(vertices);
            MeshTri[] tris = new MeshTri[vertices.Length];
            for (int i = 0; i < tris.Length; i++) {
                MeshTri tri = new MeshTri();
                tri.Tag = tag;
                tri.A = i;
                if ((i & 1) == 0) {
                    tri.B = i + 2;
                    tri.C = i + 1;
                } else {
                    tri.B = i + 1;
                    tri.C = i + 2;
                }
                tris[i] = tri;
            }
            rb.AddTris = tris;
        }
        public static void DrawClosedStrip<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> rb, TVertex[] l, TVertex[] r, object? tag = null) {
            var woven = GeometryUtils.WeaveStrip(l, r).ToList();
            woven.Add(l[0]);
            woven.Add(r[0]);
            DrawStrip(rb, woven.ToArray());
        }
        public static void DrawClosedStrip<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> rb, TVertex[] lr, object? tag = null) {
            var woven = lr.ToList();
            woven.Add(lr[0]);
            woven.Add(lr[1]);
            DrawStrip(rb, woven.ToArray());
        }

        public static void DrawStrip<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> rb, TVertex[] l, TVertex[] r, object? tag = null)
            => DrawStrip(rb, GeometryUtils.WeaveStrip(l, r));

        //TRIANGLES
        /// <summary>
        /// Draws a triangle using the specified vertices. They must be in the clockwise order to form a triangle.
        /// </summary>
        /// <param name="a">first vertex</param>
        /// <param name="b">second vertex</param>
        /// <param name="c">third vertex</param>
        public static void DrawTriangle<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> rb, TVertex a, TVertex b, TVertex c, object? tag = null) {
            int indexA = rb.AddVertex(a);
            int indexB = rb.AddVertex(b);
            int indexC = rb.AddVertex(c);
            DrawTriangleIndices(rb, indexA, indexB, indexC, tag);
        }
        public static void DrawTriangleIndices<TMaterial, TVertex>(this MeshBuilder<TMaterial, TVertex> rb, int a, int b, int c, object? tag = null) => rb.Tris.Add(new(a, b, c, tag));
    }
}
