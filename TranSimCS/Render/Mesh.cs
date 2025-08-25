using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Model;

namespace TranSimCS {
    /// <summary>
    /// A collections of meshes, each with its own texture
    /// </summary>
    public class MultiMesh {
        //List of data to render
        private Dictionary<Texture2D, Mesh> _renderBins = [];
        public IDictionary<Texture2D, Mesh> RenderBins => new ReadOnlyDictionary<Texture2D, Mesh>(_renderBins);

        //The helper method to add a render bin for a specific texture and populate it with vertices and indices.
        public Mesh GetOrCreateRenderBin(Texture2D texture) {
            return GetOrCreateRenderBin(texture, null);
        }
        public void Clear() {
            foreach (var renderBin in _renderBins.Values)
                renderBin.Clear();
        }
        public void ClearAll() {
            _renderBins.Clear();
        }
        public Mesh GetOrCreateRenderBin(Texture2D texture, Action<Mesh>? action) {
            if (!_renderBins.TryGetValue(texture, out var renderBin)) {
                renderBin = new Mesh();
                _renderBins[texture] = renderBin;
            }
            action?.Invoke(renderBin);
            return renderBin;
        }
        public void AddAll(MultiMesh meshes) {
            foreach (var kv in meshes.RenderBins) {
                IRenderBin renderBin = GetOrCreateRenderBin(kv.Key);
                renderBin.DrawModel(kv.Value);
            }
        }
    }

    public class Mesh: IRenderBin {
        public List<VertexPositionColorTexture> Vertices { get; } = new List<VertexPositionColorTexture>();
        public List<int> Indices { get; } = new List<int>();
        public IDictionary<int, object> Tags { get; } = new Dictionary<int, object>();

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

        int IRenderBin.AddVertex(VertexPositionColorTexture vertex) {
            Vertices.Add(vertex);
            return Vertices.Count - 1; // Return the index of the newly added vertex
        }

        void IRenderBin.AddIndex(int index) {
            Indices.Add(index);
        }

        public void Clear() {
            Vertices.Clear();
            Indices.Clear();
            Tags.Clear();
        }
    }
    public static class MeshUtil {
        /// <summary>
        /// Gets the intersection of a ray with a mesh.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="ray"></param>
        /// <param name="intersectionPoint"> set to location of the intersection </param>
        /// <returns>tag assigned to the intersecting triangle</returns>
        public static object RayIntersectMesh(IRenderBin mesh, Ray ray, out float intersectionDistance) {
            // This method should implement the logic to find the intersection of a ray with the mesh
            // For now, let's assume it returns null and sets intersectionPoint to Vector3.Zero
            object tag = null;
            float intersectionDistance0 = float.MaxValue; // Initialize max distance to a large value
            for (int i = 0; i < mesh.Indices.Count; i += 3) {
                // Get the vertices of the triangle
                var v0 = mesh.Vertices[mesh.Indices[i]].Position;
                var v1 = mesh.Vertices[mesh.Indices[i + 1]].Position;
                var v2 = mesh.Vertices[mesh.Indices[i + 2]].Position;
                // Check for intersection with the triangle
                if (Geometry.RayIntersectsTriangle(ray, v0, v1, v2, out float thisIntersectionDistance)) {
                    // Return the tag associated with the triangle
                    // Assuming the tag is stored in the mesh.Tags dictionary with the triangle index as the key
                    object potentialTag = mesh.Tags.ContainsKey(i / 3) ? mesh.Tags[i / 3] : null;
                    if (thisIntersectionDistance < intersectionDistance0) {
                        intersectionDistance0 = thisIntersectionDistance; // Update the intersection point
                        tag = potentialTag; // Update the tag
                    }
                }
            }
            intersectionDistance = intersectionDistance0;
            return tag; // No intersection found
        }

        public static object RayIntersectMeshes(IEnumerable<IRenderBin> meshes, Ray ray, out float intersectionDistance) {
            object tag = null;
            float intersectionDistance0 = float.MaxValue; // Initialize max distance to a large value
            foreach (IRenderBin mesh in meshes) {
                object tag1 = RayIntersectMesh(mesh, ray, out var intersectionDistance1);
                if (intersectionDistance1 < intersectionDistance0) {
                    intersectionDistance0 = intersectionDistance1;
                    tag = tag1;
                }
            }
            intersectionDistance = intersectionDistance0;
            return tag;
        }
    }
}
