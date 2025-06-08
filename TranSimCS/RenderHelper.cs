using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS {
    internal class RenderHelper {
        public GraphicsDevice GraphicsDevice { get; private init; }
        public BasicEffect Effect { get; private init; }

        //List of data to render
        private Dictionary<Texture, RenderBin> _renderBins = new();

        public RenderHelper(GraphicsDevice graphicsDevice) {
            GraphicsDevice = graphicsDevice;
            Effect = new BasicEffect(graphicsDevice) {
                VertexColorEnabled = true,
                TextureEnabled = false,
                LightingEnabled = false
            };
        }

        //The helper method to add a render bin for a specific texture and populate it with vertices and indices.
        public RenderBin GetOrCreateRenderBin(Texture texture) {
            return GetOrCreateRenderBin(texture, null);
        }
        public void Clear() {
            foreach (var renderBin in _renderBins.Values) {
                renderBin.Clear();
            }
        }
        public RenderBin GetOrCreateRenderBin(Texture texture, Action<RenderBin>? action) {
            if (!_renderBins.TryGetValue(texture, out var renderBin)) {
                renderBin = new RenderBin(this);
                _renderBins[texture] = renderBin;
            }
            action?.Invoke(renderBin);
            return renderBin;
        }



        public void Render() {
            foreach (var renderBin in _renderBins.Values) {
                if (renderBin.Vertices.Count == 0 || renderBin.Indices.Count == 0) continue;
                GraphicsDevice.SetVertexBuffer(new VertexBuffer(GraphicsDevice, typeof(VertexPositionColorTexture), renderBin.Vertices.Count, BufferUsage.WriteOnly));
                GraphicsDevice.Indices = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, renderBin.Indices.Count, BufferUsage.WriteOnly);
                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, renderBin.Vertices.ToArray(), 0, renderBin.Vertices.Count, renderBin.Indices.ToArray(), 0, renderBin.Indices.Count / 3);
            }
        }
    }

    /// <summary>
    /// A render bin is a collection of vertices and indices that can be rendered together.
    /// Each render bin is associated with a specific texture and uses a RenderHelper for rendering.
    /// </summary>
    internal class RenderBin {
        public RenderHelper RenderHelper { get; private init; }
        public List<VertexPositionColorTexture> Vertices { get; private init; } = [];
        public List<int> Indices { get; private init; } = [];
        public RenderBin(RenderHelper renderHelper) {
            RenderHelper = renderHelper;
        }
        public void Clear() {
            Vertices.Clear();
            Indices.Clear();
        }
        public int AddVertex(VertexPositionColorTexture vertex) {
            Vertices.Add(vertex);
            return Vertices.Count - 1;
        }
        public void AddIndex(int index) {
            Indices.Add(index);
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
        public void DrawModel(VertexPositionColorTexture[] vertices, int[] indices) {
            ArgumentNullException.ThrowIfNull(vertices);
            ArgumentNullException.ThrowIfNull(indices);
            int[] newVertexIds = new int[indices.Length];
            for (int i = 0; i < vertices.Length; i++)
                newVertexIds[i] = AddVertex(vertices[i]);
            foreach (var index in indices)
                AddIndex(newVertexIds[index]);
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
        /// The vertices should start at the bottom left and go in a zigzag pattern to the top right.
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
                AddIndex(newVertexIds[i + 2]);
                AddIndex(newVertexIds[i + 1]);
            }
        }
    }
}
