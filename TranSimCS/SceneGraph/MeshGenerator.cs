using System;
using TranSimCS.Model;
using TranSimCS.Worlds;

namespace TranSimCS.SceneGraph {
    /// <summary>
    /// Generates and manages meshes for objects
    /// </summary>
    /// <param name="obj">object to assign this mesh generator to</param>
    /// <param name="func">mesh generating function</param>
    public class MeshGenerator<T>
        where T : Obj, IObjMesh {
        public event Action<MultiMesh>? OnMeshGenerated;
        public event Action? OnMeshInvalidated;
        private MultiMesh? mesh;
        public readonly T obj;
        public readonly Action<T, MultiMesh> func;

        public MeshGenerator(T obj, Action<T, MultiMesh> func) {
            this.obj = obj;
            this.func = func;
            obj.DependencyChanged += (s, o, n) => Invalidate();
        }

        public MultiMesh GetMesh() {
            if (mesh == null) {
                mesh = new MultiMesh();
                func(obj, mesh);
                OnMeshGenerated?.Invoke(mesh);
            }
            return mesh;
        }
        public void Invalidate() {
            if (mesh != null) {
                mesh = null;
                OnMeshInvalidated?.Invoke();
            }
        }
    }
}
