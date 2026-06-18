using TranSimCS.Model;
using TranSimCS.SceneGraph;

namespace TranSimCS.Worlds {
    public interface IObjMesh {
        public MultiMesh GetMesh();
        public void InvalidateMesh();
    }
    public interface IObjMesh<T>: IObjMesh where T: Obj, IObjMesh<T> {
        public MeshGenerator<T> Mesh { get; }
        MultiMesh IObjMesh.GetMesh() => Mesh.GetMesh();
        void IObjMesh.InvalidateMesh() => Mesh.Invalidate();
    }
}
