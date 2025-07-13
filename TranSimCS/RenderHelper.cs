using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS {
    public class RenderHelper: MultiMesh {
        public GraphicsDevice GraphicsDevice { get; private init; }
        public BasicEffect Effect { get; private init; }

        public RenderHelper(GraphicsDevice graphicsDevice) {
            GraphicsDevice = graphicsDevice;
            Effect = new BasicEffect(graphicsDevice) {
                VertexColorEnabled = true,
                TextureEnabled = false,
                LightingEnabled = false
            };
        }
        public RenderHelper(GraphicsDevice graphicsDevice, BasicEffect effect) {
            GraphicsDevice = graphicsDevice;
            Effect = effect ?? throw new ArgumentNullException(nameof(effect), "Effect cannot be null. Please provide a valid BasicEffect instance.");
        }

        public void Render() {
            int TriCount = 0;
            int VertCount = 0;
            GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
            foreach (var row in RenderBins) {
                var renderBin = row.Value;
                var texture = row.Key;
                Effect.Texture = texture;
                TriCount += (renderBin.Indices.Count) / 3;
                VertCount += renderBin.Vertices.Count;
                if (renderBin.Vertices.Count == 0 || renderBin.Indices.Count == 0) continue;
                foreach (var pass in Effect.CurrentTechnique.Passes) {
                    pass.Apply();
                    GraphicsDevice.SetVertexBuffer(new VertexBuffer(GraphicsDevice, typeof(VertexPositionColorTexture), renderBin.Vertices.Count, BufferUsage.WriteOnly));
                    GraphicsDevice.Indices = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, renderBin.Indices.Count, BufferUsage.WriteOnly);
                    GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, renderBin.Vertices.ToArray(), 0, renderBin.Vertices.Count, renderBin.Indices.ToArray(), 0, renderBin.Indices.Count / 3);
                }
            }
        }
    }

    /// <summary>
    /// Defines an interface for a render bin, which serves as a container for managing vertices and indices used in
    /// rendering operations.
    /// </summary>
    /// <remarks>A render bin provides methods for adding vertices and indices, as well as drawing various
    /// shapes and models. It is designed to facilitate rendering operations by organizing vertex and index data
    /// efficiently.</remarks>
    public interface IRenderBin {
        /// <summary>
        /// Gets the list of vertices in this render bin.
        /// </summary>
        List<VertexPositionColorTexture> Vertices { get; }
        /// <summary>
        /// Gets the list of indices in this render bin.
        /// </summary>
        List<int> Indices { get; }
        /// <summary>
        /// Tags the triangles in this render bin with an integer key corresponding to the triangle's number and an object value.
        /// </summary>
        IDictionary<int, object> Tags { get; }

        public int AddVertex(VertexPositionColorTexture vertex);
        public void AddIndex(int index);
        public void Clear();
        public void AddTagsToLastTriangles(int count, object value) {
            if (value == null) return;
            ArgumentOutOfRangeException.ThrowIfNegative(count, nameof(count));
            if (count == 0) return;
            int startIndex = (Indices.Count / 3) - count; // Each triangle has 3 indices
            for (int i = 0; i < count; i++) {
                Tags[startIndex + i] = value;
            }
        }

        //Rendering methods for different shapes and primitives
        /// <summary>
        /// Draws a quad using the specified vertices. They must be in the clockwise order to form a quad.
        /// </summary>
        /// <param name="a">first vertex</param>
        /// <param name="b">second verted</param>
        /// <param name="c">third vertex</param>
        /// <param name="d">fourth vertex</param>
        private static readonly int[] indexDataQuadLookup = [0, 1, 2, 0, 2, 3];
        public void DrawQuad(VertexPositionColorTexture a, VertexPositionColorTexture b, VertexPositionColorTexture c, VertexPositionColorTexture d) {
            int indexA = AddVertex(a);
            int indexB = AddVertex(b);
            int indexC = AddVertex(c);
            int indexD = AddVertex(d);
            int[] indexDataQuad = [indexA, indexB, indexC, indexD];
            foreach (var index in indexDataQuadLookup) {
                AddIndex(indexDataQuad[index]);
            }
        }
        public void DrawQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color) {
            DrawQuad(
                new(a, color, new(0, 0)),
                new(b, color, new(1, 0)),
                new(c, color, new(1, 1)),
                new(d, color, new(0, 1)));
        }
        public void DrawQuad(Quad q) => DrawQuad(q.a, q.b, q.c, q.d);

        /// <summary>
        /// Draws a triangle using the specified vertices. They must be in the clockwise order to form a triangle.
        /// </summary>
        /// <param name="a">first vertex</param>
        /// <param name="b">second vertex</param>
        /// <param name="c">third vertex</param>
        public void DrawTriangle(VertexPositionColorTexture a, VertexPositionColorTexture b, VertexPositionColorTexture c) {
            int indexA = AddVertex(a);
            int indexB = AddVertex(b);
            int indexC = AddVertex(c);
            AddIndex(indexA);
            AddIndex(indexB);
            AddIndex(indexC);
        }
        /// <summary>
        /// Draws a model using the specified vertices and indices.
        /// </summary>
        /// <param name="vertices">List of vertices</param>
        /// <param name="indices">List of indices</param>
        public void DrawModel(IList<VertexPositionColorTexture> vertices, IList<int> indices) {
            ArgumentNullException.ThrowIfNull(vertices);
            ArgumentNullException.ThrowIfNull(indices);
            int[] newVertexIds = new int[indices.Count];
            for (int i = 0; i < vertices.Count; i++)
                newVertexIds[i] = AddVertex(vertices[i]);
            foreach (var index in indices)
                AddIndex(newVertexIds[index]);
        }
        public void DrawModel(Mesh mesh) {
            ArgumentNullException.ThrowIfNull(mesh);
            DrawModel(mesh.Vertices, mesh.Indices);
        }

        /// <summary>
        /// Draws a grid of vertices, where each square is made up of two triangles.
        /// The vertices should be in a 2D array, where each element is a VertexPositionColorTexture.
        /// The Elements should be in the order of (x, y), where x is the horizontal index and y is the vertical index.
        /// The elements should be in the clockwise order to form a rectangular mesh.
        /// </summary>
        /// <param name="vertices"></param>
        public void DrawGrid(VertexPositionColorTexture[,] vertices) {
            ArgumentNullException.ThrowIfNull(vertices);
            int height = vertices.GetLength(1);
            int width = vertices.GetLength(0);
            int[,] newVertexIds = new int[width, height];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    newVertexIds[i, j] = AddVertex(vertices[i, j]);
            for (int i = 0; i < width - 1; i++) {
                for (int j = 0; j < height - 1; j++) {
                    // Draw two triangles for each square in the grid
                    AddIndex(newVertexIds[i, j]);
                    AddIndex(newVertexIds[i + 1, j]);
                    AddIndex(newVertexIds[i, j + 1]);
                    AddIndex(newVertexIds[i + 1, j + 1]);
                    AddIndex(newVertexIds[i, j + 1]);
                    AddIndex(newVertexIds[i + 1, j]);
                }
            }
        }

        /// <summary>
        /// Draws a strip of vertices, where each vertex is connected to the next one in a triangle strip fashion.
        /// The vertices should start at the bottom left and even positions are the left vertices, odd positions are the right vertices.
        /// </summary>
        /// <param name="vertices"></param>
        /// <exception cref="ArgumentException"></exception>
        public void DrawStrip(VertexPositionColorTexture[] vertices) {
            ArgumentNullException.ThrowIfNull(vertices);
            if (vertices.Length < 3) throw new ArgumentException("At least three vertices are required to draw a strip.");
            int[] newVertexIds = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                newVertexIds[i] = AddVertex(vertices[i]);
            for (int i = 0; i < newVertexIds.Length - 2; i++) {
                AddIndex(newVertexIds[i]);
                if ((i & 1) == 0) {
                    AddIndex(newVertexIds[i + 2]);
                    AddIndex(newVertexIds[i + 1]);
                } else {
                    AddIndex(newVertexIds[i + 1]);
                    AddIndex(newVertexIds[i + 2]);
                }

            }
        }
    }
}
