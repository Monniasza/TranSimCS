using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Worlds;

namespace TranSimCS.Model {
    public struct MeshObject {
        public ObjPos pos;
        public MeshComplex mesh;

        public MeshObject(MeshComplex mesh) {
            this.mesh = mesh;
        }
        public MeshObject(MeshComplex mesh, ObjPos pos) {
            this.mesh = mesh;
            this.pos = pos;
        }

        public bool LookUp(Ray ray, out float T, out object? tag) {
            var transform = pos.CalcReferenceMatrix();
            var inverseTransform = Matrix.Invert(transform);

            var invertedRay = RayMethods.Transform(ray, inverseTransform);
            var lookup = MeshUtil.RayIntersectMeshes(mesh.Elements.Values, invertedRay, out T);
            tag = lookup;
            return T < float.MaxValue;
        }
        public void Render(MeshComplex target) {
            var refframe = pos.CalcReferenceFrame();
            refframe.TransformOutOfPlace(mesh, target);
        }
    }
}
