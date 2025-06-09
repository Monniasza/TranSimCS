using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS {
    public class Mesh: IRenderBin {
        public List<VertexPositionColorTexture> Vertices { get; } = new List<VertexPositionColorTexture>();
        public List<int> Indices { get; } = new List<int>();
        public IDictionary<int, object> Tags { get; } = new Dictionary<int, object>();

        RenderHelper IRenderBin.RenderHelper => throw new NotImplementedException();

        public Mesh() { }
        public Mesh(IEnumerable<VertexPositionColorTexture> vertices, IEnumerable<int> indices) {
            Vertices.AddRange(vertices);
            Indices.AddRange(indices);
        }

        int IRenderBin.AddVertex(VertexPositionColorTexture vertex) {
            Vertices.Add(vertex);
            return Vertices.Count - 1; // Return the index of the newly added vertex
        }

        void IRenderBin.AddIndex(int index) {
            Indices.Add(index);
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
                    if (potentialTag == null) {
                        Debug.WriteLine($"No tag found for triangle at index {i / 3} in mesh.");
                        continue;
                    }
                    if (thisIntersectionDistance < intersectionDistance0) {
                        intersectionDistance0 = thisIntersectionDistance; // Update the intersection point
                        tag = potentialTag; // Update the tag
                    } else {
                        Debug.WriteLine($"The triangle is further {i / 3} in mesh.");
                    }
                }
            }
            intersectionDistance = intersectionDistance0;
            return tag; // No intersection found
        }
    }
}
