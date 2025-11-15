using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Model {
    /// <summary>
    /// A mesh triangle. Also includes a tag for selection purposes.
    /// </summary>
    public struct MeshTri {
        /// <summary>
        /// First vertex ID
        /// </summary>
        public int A;
        /// <summary>
        /// Second vertex ID
        /// </summary>
        public int B;
        /// <summary>
        /// Third vertex ID
        /// </summary>
        public int C;
        /// <summary>
        /// Selection tag. null for no selection.
        /// </summary>
        public object? Tag;

        public MeshTri(int a, int b, int c, object? tag = null) {
            A = a; B = b; C = c; Tag = tag;
        }

        public static MeshTri operator +(MeshTri triangle, int offset) => new MeshTri(triangle.A + offset, triangle.B + offset, triangle.C + offset, triangle.Tag);

        public static MeshTri[] FromArray(int[] data, object? tag = null) {
            var tricount = data.Length / 3;
            var tris = new MeshTri[tricount];
            for (int i = 0; i < tricount; i++) {
                var a = data[i*3];
                var b = data[i * 3 + 2];
                var c = data[i * 3 + 1];
                tris[i] = new(a, b, c, tag);
            }
    }
}
