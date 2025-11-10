using System;
using TranSimCS.Model;
using TranSimCS.Worlds;

namespace TranSimCS.SceneGraph {
    /// <summary>
    /// Generates and manages meshes for objects
    /// </summary>
    /// <param name="obj">object to assign this mesh generator to</param>
    /// <param name="func">mesh generating function</param>
    public class MeshGenerator<T>: IMeshSource
        where T : Obj, IObjMesh<T> {
        public event Action<MultiMesh>? OnMeshGenerated;
        public event Action? OnMeshInvalidated;
        private MultiMesh? mesh;
        public readonly SceneLeaf Leaf;
        public readonly T obj;
        public readonly Action<T, MultiMesh> func;

        public MeshGenerator(T obj, Action<T, MultiMesh> func) {
            Leaf = new SceneLeaf(this);
            this.obj = obj;
            this.func = func;
            obj.PropertyChanged += (s, e) => Invalidate();
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
            mesh = null;
            OnMeshInvalidated?.Invoke();
        }
    }
}
