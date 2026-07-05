using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Spatial;

namespace TranSimCS.Model {

    public class Mesh: IBVHElement{
        public List<VertexPositionColorTexture> Vertices { get; } = [];
        public List<ushort> Indices { get; } = [];
        public IDictionary<int, object> Tags { get; } = new Dictionary<int, object>();
        public readonly MultiMesh? Parent;


        private MeshBvh? bvh;
        public int GeometryVersion { get; private set; }
        internal MeshBvh GetAccelerationStructure() => bvh ??= MeshBvh.Build(this);
        internal void InvalidateAccelerationStructure(){
            bvh = null;
            GeometryVersion++;
            Parent?.InvalidateAccelerationStructure();
        }
        public BoundingBox GetBounds() => GetAccelerationStructure().Bounds;

        public bool ComputeIntersection(Ray ray, out float distance, out object? tag) {
            tag = MeshUtil.RayIntersectMesh(this, ray, out distance);
            return distance < float.MaxValue;
        }

        public Mesh(MultiMesh? parent = null, IEnumerable<VertexPositionColorTexture>? vertices = null, IEnumerable<ushort>? indices = null, IDictionary<int, object>? tags = null) {
            this.Parent = parent;
            if(vertices != null) Vertices.AddRange(vertices);
            if(indices != null) Indices.AddRange(indices);
            if(tags != null) foreach(var row in tags) Tags.Add(row.Key, row.Value);
        }
        public ushort AddVertex(VertexPositionColorTexture vertex) {
            Vertices.Add(vertex);
            InvalidateAccelerationStructure();
            return (ushort)(Vertices.Count - 1); // Return the index of the newly added vertex
        }
        public void AddIndex(ushort index) {
            Indices.Add(index);
            InvalidateAccelerationStructure();
        }
        public void AddIndices(ushort[] indices) {
            Indices.AddRange(indices);
            InvalidateAccelerationStructure();
        }

        public int AddVerts(VertexPositionColorTexture[] verts) {
            int index = Vertices.Count;
            Vertices.AddRange(verts);
            InvalidateAccelerationStructure();
            return index;
        }

        public void Clear() {
            Vertices.Clear();
            Indices.Clear();
            Tags.Clear();
            InvalidateAccelerationStructure();
        }
        public void DrawTriangle(ushort a, ushort b, ushort c) {
            AddIndex(a);
            AddIndex(b);
            AddIndex(c);
        }

        /// <summary>
        /// Draws a model using the specified vertices and indices.
        /// </summary>
        /// <param name="vertices">List of vertices</param>
        /// <param name="indices">List of indices</param>
        public void DrawModel(IList<VertexPositionColorTexture> vertices, IList<ushort> indices, IEnumerable<KeyValuePair<int, object>>? tags = null) {
            ArgumentNullException.ThrowIfNull(vertices, nameof(vertices));
            ArgumentNullException.ThrowIfNull(indices, nameof(indices));
            int startVertexId = AddVerts(vertices.ToArray());
            int startingIndex = Indices.Count;
            int startingTriCount = startingIndex / 3;
            var indicesArray = indices.ToArray();
            for (int i = 0; i < indicesArray.Length; i++) {
                indicesArray[i] += (ushort)startVertexId;
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
