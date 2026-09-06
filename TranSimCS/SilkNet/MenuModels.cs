using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;

namespace TranSimCS.SilkNet {
    public static class MenuModels {
        public static Mesh BillboardVertical { get; } = CreateBillboard(1, 2);

        private static Mesh CreateBillboard(float width, float height) {
            float hwidth = width / 2;
            Vertex[] verts = {
                new(new(-hwidth, height, 0), new(0, 0)),
                new(new( hwidth, height, 0), new(1, 0)),
                new(new( hwidth,      0, 0), new(1, 1)),
                new(new(-hwidth,      0, 0), new(0, 1))
            };
            ushort[] indices = { 0, 1, 2, 0, 2, 3 };
            return new Mesh(vertices: verts, indices: indices);
        }
    }
}
