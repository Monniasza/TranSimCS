using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Model {
    public static class MeshElementMethods {
        public static MeshElement<TMaterial, T2> Transform<TMaterial, T1, T2>(this MeshElement<TMaterial, T1> element, Func<T1, T2> fn) {
            var verts = new T2[element.Vertices.Length];
            var tris = element.Triangles.ToArray();
            for (int i = 0; verts.Length > i; i++) {
                verts[i] = fn(element.Vertices[i]);
            }
            return new MeshElement<TMaterial, T2>(element.Name, element.Material, verts, tris, element.IsVisible);
        }
    }
}
