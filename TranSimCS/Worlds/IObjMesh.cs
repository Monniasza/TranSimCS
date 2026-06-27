using TranSimCS.Model;
using TranSimCS.SceneGraph;

namespace TranSimCS.Worlds {
    public interface IObjMesh {
        public MultiMesh GetMesh();
        public MultiMesh? GetSelection0();
        public void InvalidateMesh();
    }
    public interface IObjMesh<T>: IObjMesh where T: Obj, IObjMesh<T> {
        public MeshGenerator<T> Mesh { get; }
        public MeshGenerator<T>? SelectionMesh { get => null; }
        MultiMesh IObjMesh.GetMesh() => Mesh.GetMesh();
        void IObjMesh.InvalidateMesh() => Mesh.Invalidate();
        MultiMesh? IObjMesh.GetSelection0() => SelectionMesh?.GetMesh();
    }

    public static class ObjMeshMethods {
        public static MultiMesh GetSelection(this IObjMesh obj)
            => obj.GetSelection0() ?? obj.GetMesh();
    }
}
