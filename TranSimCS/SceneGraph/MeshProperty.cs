using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Property;
using TranSimCS.Worlds;

namespace TranSimCS.SceneGraph {
    public class MeshProperty : Obj, IObjMesh {
        private Property<MultiMesh> prop;
        public event MeshInvalidationCallback GeometryChanged;

        public MeshProperty(Property<MultiMesh> prop) {
            this.prop = prop;
            prop.ValueChanged += Prop_ValueChanged;
        }

        private void Prop_ValueChanged(object? sender, MultiMesh old, MultiMesh value) => GeometryChanged(this);
        public void GenerateGeometry(RenderTarget target) => target.Draw(prop.Value);
        public AABB GetBounds() => prop.Value.GetBounds();
        public bool ComputeIntersection(Ray3 ray, out float distance, out object? tag) => prop.Value.ComputeIntersection(ray, out distance, out tag);
    }
}
