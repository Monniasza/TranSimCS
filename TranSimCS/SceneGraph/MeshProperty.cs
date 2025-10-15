using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;
using TranSimCS.Worlds;

namespace TranSimCS.SceneGraph {
    public class MeshProperty : IMeshSource {
        private Property<MultiMesh> prop;
        public event Action<MultiMesh>? OnMeshGenerated;
        public event Action? OnMeshInvalidated;

        public MeshProperty(Property<MultiMesh> prop) {
            this.prop = prop;
            prop.ValueChanged += Prop_ValueChanged;
        }

        private void Prop_ValueChanged(object? sender, PropertyChangedEventArgs2<MultiMesh> e) {
            if (e.OldValue != null) OnMeshInvalidated?.Invoke();
            if (e.NewValue != null) OnMeshGenerated?.Invoke(e.NewValue);
        }

        public MultiMesh GetMesh() {
            return prop.Value;
        }

        public void Invalidate() {}
    }
}
