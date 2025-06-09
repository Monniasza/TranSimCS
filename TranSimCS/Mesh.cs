using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS {
    public class Mesh {
        public List<VertexPositionColorTexture> Vertices { get; } = new List<VertexPositionColorTexture>();
        public List<int> Indices { get; } = new List<int>();
        public Mesh() { }
        public Mesh(IEnumerable<VertexPositionColorTexture> vertices, IEnumerable<int> indices) {
            Vertices.AddRange(vertices);
            Indices.AddRange(indices);
        }
    }
}
