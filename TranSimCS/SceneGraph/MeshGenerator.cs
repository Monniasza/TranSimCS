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
        where T : Obj, IObjMesh<T> {
        public event Action<T, MultiMesh>? OnMeshGenerated;
        public event Action<T>? OnRemoveMesh;
        private MultiMesh? mesh;
        public readonly SceneLeaf<T> Leaf;
        public readonly T obj;
        public readonly Action<T, MultiMesh> func;

        public MeshGenerator(T obj, Action<T, MultiMesh> func) {
            Leaf = new SceneLeaf<T>(this);
            this.obj = obj;
            this.func = func;
        }

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
