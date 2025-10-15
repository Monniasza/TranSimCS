using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;

namespace TranSimCS.SceneGraph {
    public interface IMeshSource {
        public event Action<MultiMesh>? OnMeshGenerated;
        public event Action? OnMeshInvalidated;

        public MultiMesh GetMesh();
        public void Invalidate();
    }
}
