using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;
using TranSimCS.Worlds.Property;

namespace TranSimCS.SceneGraph {
    public class MeshProperty : IMeshSource {
        private Property<MeshComplex> prop;
        public event Action<MeshComplex>? OnMeshGenerated;
        public event Action? OnMeshInvalidated;

        public MeshProperty(Property<MeshComplex> prop) {
            this.prop = prop;
            prop.ValueChanged += Prop_ValueChanged;
        }

        private void Prop_ValueChanged(object? sender, PropertyChangedEventArgs2<MeshComplex> e) {
            if (e.OldValue != null) OnMeshInvalidated?.Invoke();
            if (e.NewValue != null) OnMeshGenerated?.Invoke(e.NewValue);
        }

        public MeshComplex GetMesh() {
            return prop.Value;
        }

        public void Invalidate() {}
    }
}
