using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Model {

    public class Mesh{
        public List<VertexPositionColorTexture> Vertices { get; } = new List<VertexPositionColorTexture>();
        public List<int> Indices { get; } = new List<int>();
        public IDictionary<int, object> Tags { get; } = new Dictionary<int, object>();


        private MeshBvh? bvh;
        internal MeshBvh GetAccelerationStructure() => bvh ??= MeshBvh.Build(this);
        internal void InvalidateAccelerationStructure() => bvh = null;

        public Mesh() { }
        public Mesh(IEnumerable<VertexPositionColorTexture> vertices, IEnumerable<int> indices) {
            Vertices.AddRange(vertices);
            Indices.AddRange(indices);
        }
        public Mesh(IEnumerable<VertexPositionColorTexture> vertices, IEnumerable<int> indices, IDictionary<int, object> tags) {
            Vertices.AddRange(vertices);
            Indices.AddRange(indices);
            foreach(var row in tags) Tags.Add(row.Key, row.Value);
        }
        public int AddVertex(VertexPositionColorTexture vertex) {
            Vertices.Add(vertex);
            InvalidateAccelerationStructure();
            return Vertices.Count - 1; // Return the index of the newly added vertex
        }
        public void AddIndex(int index) {
            Indices.Add(index);
            InvalidateAccelerationStructure();
        }
        public void AddIndices(int[] indices) => Indices.AddRange(indices);

        public int AddVerts(VertexPositionColorTexture[] verts) {
            int index = Vertices.Count;
            Vertices.AddRange(verts);
            return index;

        }

        public void Clear() {
            Vertices.Clear();
            Indices.Clear();
            Tags.Clear();
            InvalidateAccelerationStructure();
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
            int startVertexId = AddVerts(vertices.ToArray());
            int startingIndex = Indices.Count;
            int startingTriCount = startingIndex / 3;
            var indicesArray = indices.ToArray();
            for (int i = 0; i < indicesArray.Length; i++) {
                indicesArray[i] += startVertexId;
            }
            AddIndices(indicesArray);
            foreach (var kv in tags ?? []) {
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
