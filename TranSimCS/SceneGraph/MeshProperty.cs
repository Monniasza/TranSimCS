using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
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

        private void Prop_ValueChanged(object? sender, PropertyChangedEventArgs2<MultiMesh> e) => GeometryChanged(this);
        public void GenerateGeometry(RenderTarget target) => target.Draw(prop.Value);
        public BoundingBox GetBounds() => prop.Value.GetBounds();
        public bool ComputeIntersection(Ray ray, out float distance, out object? tag) => prop.Value.ComputeIntersection(ray, out distance, out tag);
    }
}
