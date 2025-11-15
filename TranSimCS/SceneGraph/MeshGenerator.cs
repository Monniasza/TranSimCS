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
        public event Action<MeshComplex>? OnMeshGenerated;
        public event Action? OnMeshInvalidated;
        private MeshComplex? mesh;
        public readonly SceneLeaf Leaf;
        public readonly T obj;
        public readonly Action<T, MeshComplex> func;

        public MeshGenerator(T obj, Action<T, MeshComplex> func) {
            Leaf = new SceneLeaf(this);
            this.obj = obj;
            this.func = func;
            obj.PropertyChanged += (s, e) => Invalidate();
        }

        public MeshComplex GetMesh() {
            if (mesh == null) {
                mesh = new MeshComplex();
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
