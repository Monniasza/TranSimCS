using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS {
    public class Mesh: IRenderBin {
        public List<VertexPositionColorTexture> Vertices { get; } = new List<VertexPositionColorTexture>();
        public List<int> Indices { get; } = new List<int>();

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
}
