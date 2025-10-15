using TranSimCS.SceneGraph;

namespace TranSimCS.Worlds {
    public interface IObjMesh<T> where T: Obj, IObjMesh<T> {
        public MeshGenerator<T> Mesh { get; }
    }
}
