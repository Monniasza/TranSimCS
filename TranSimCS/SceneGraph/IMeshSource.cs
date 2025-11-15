using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;

namespace TranSimCS.SceneGraph {
    public interface IMeshSource {
        public event Action<MeshComplex>? OnMeshGenerated;
        public event Action? OnMeshInvalidated;

        public MeshComplex GetMesh();
        public void Invalidate();
    }
}
