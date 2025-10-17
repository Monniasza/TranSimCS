using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Model {
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

        public void DrawTriangle(int a, int b, int c) {
            AddIndex(a);
            AddIndex(b);
            AddIndex(c);
        }

        /// <summary>
        /// Draws a model using the specified vertices and indices.
        /// </summary>
        /// <param name="vertices">List of vertices</param>
        /// <param name="indices">List of indices</param>
        public void DrawModel(IList<VertexPositionColorTexture> vertices, IList<int> indices, IEnumerable<KeyValuePair<int, object>>? tags = null) {
            ArgumentNullException.ThrowIfNull(vertices, nameof(vertices));
            ArgumentNullException.ThrowIfNull(indices, nameof(indices));
            int[] newVertexIds = new int[vertices.Count];
            int startingIndex = Indices.Count;
            int startingTriCount = startingIndex / 3;
            for (int i = 0; i < vertices.Count; i++)
                newVertexIds[i] = AddVertex(vertices[i]);
            foreach (var index in indices)
                AddIndex(newVertexIds[index]);
            foreach(var kv in tags ?? []) {
                var newTriId = startingTriCount + kv.Key;
                Tags.Add(newTriId, kv.Value);
            }

        }
        public void DrawModel(Mesh mesh) {
            ArgumentNullException.ThrowIfNull(mesh);
            DrawModel(mesh.Vertices, mesh.Indices, mesh.Tags);

        }
    }
}
