using System;
using TranSimCS.Model;

namespace TranSimCS.Worlds {
    public interface IObjMesh<T> where T: Obj, IObjMesh<T> {
        public MeshGenerator<T> Mesh { get; }
    }

    /// <summary>
    /// Generates and manages meshes for objects
    /// </summary>
    /// <param name="obj">object to assign this mesh generator to</param>
    /// <param name="func">mesh generating function</param>
    public class MeshGenerator<T>(T obj, Action<T, MultiMesh> func)
        where T : Obj, IObjMesh<T> {
        public event Action<T, MultiMesh>? OnMeshGenerated;
        public event Action<T>? OnRemoveMesh;
        private MultiMesh? mesh;

        public MultiMesh GetMesh() {
            if (mesh == null) {
                mesh = new MultiMesh();
                func(obj, mesh);
                OnMeshGenerated?.Invoke(obj, mesh);
            }
            return mesh;
        }
        public void Invalidate() {
            mesh = null;
            OnRemoveMesh?.Invoke(obj);
        }
    }
}
