using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Model {

    public class Mesh: IRenderBin {
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

        int IRenderBin.AddVertex(VertexPositionColorTexture vertex) => AddVertex(vertex);
        public int AddVertex(VertexPositionColorTexture vertex) {
            Vertices.Add(vertex);
            InvalidateAccelerationStructure();
            return Vertices.Count - 1; // Return the index of the newly added vertex
        }

        void IRenderBin.AddIndex(int index) => AddIndex(index);
        public void AddIndex(int index) {
            Indices.Add(index);
            InvalidateAccelerationStructure();
        }

        void IRenderBin.AddIndices(int[] indices) => AddIndices(indices);
        public void AddIndices(int[] indices) => Indices.AddRange(indices);

        int IRenderBin.AddVerts(VertexPositionColorTexture[] verts) => AddVerts(verts);
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
    }
}
