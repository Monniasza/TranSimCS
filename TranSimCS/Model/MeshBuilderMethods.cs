using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        public static MeshBuilder<TMaterial, VertexPositionColorTexture> DrawQuadUV<TMaterial>(this MeshBuilder<TMaterial, VertexPositionColorTexture> meshBuilder, Color color, Vector3 a, Vector3 b, Vector3 c, Vector3 d, object? tag = null) {
            return DrawQuad(meshBuilder, [
                new VertexPositionColorTexture(a, color, new(0, 0)),
                new VertexPositionColorTexture(b, color, new(1, 0)),
                new VertexPositionColorTexture(c, color, new(1, 1)),
                new VertexPositionColorTexture(d, color, new(0, 1))
            ], tag);
        }

        //PARALLELOGRAMS (POS/COL/UV?)
        public static MeshBuilder<TMaterial, VertexPositionColorTexture> DrawParallelogram<TMaterial>(this MeshBuilder<TMaterial, VertexPositionColorTexture> meshBuilder, Vector3 a, Vector3 x, Vector3 y, Color c, float width = 0.2f) {
            var p1 = a;
            var p2 = a + x;
            var p3 = p2 + y;
            var p4 = a + y;
            return DrawQuadUV(meshBuilder, c, p1, p2, p3, p4);
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
    }
}
