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
        /// <summary>
        /// Gets the actual selection mesh from a meshed object.
        /// If its <see cref="IObjMesh.GetSelection0()"/> returns <see langword="null"/>, the method will return <paramref name="obj"/>'s <see cref="IObjMesh.GetMesh()"/>.
        /// Else it returns its <see cref="IObjMesh.GetSelection0()"/>
        /// </summary>
        /// <param name="obj">a renderable object to get its authoritative selection mesh from</param>
        /// <returns>the authoritative selection mesh for the supplied <paramref name="obj"/></returns>
        public static MultiMesh GetSelection(this IObjMesh obj)
            => obj.GetSelection0() ?? obj.GetMesh();
        /// <summary>
        /// Gets the actual selection mesh generator from a meshed object.
        /// If its <see cref="IObjMesh{T}.SelectionMesh"/> returns <see langword="null"/>, the method will return <paramref name="obj"/>'s <see cref="IObjMesh{T}.Mesh"/>.
        /// Else it returns its <see cref="IObjMesh{T}.SelectionMesh"/>
        /// </summary>
        /// <typeparam name="T">type of the meshed object</typeparam>
        /// <param name="obj">a renderable object to get its authoritative selection mesh generator from</param>
        /// <returns>the authoritative selection mesh generator for the supplied <paramref name="obj"/></returns>
        public static MeshGenerator<T> GetSelectionMeshGenerator<T>(this T obj) where T : Obj, IObjMesh<T>
            => obj.SelectionMesh ?? obj.Mesh;
    }
}
