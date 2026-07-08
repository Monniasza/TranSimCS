using TranSimCS.Model;
using TranSimCS.SceneGraph;
using TranSimCS.Spatial;
using static TranSimCS.Model.MeshUnroll;

namespace TranSimCS.Worlds {
    public delegate void MeshInvalidationCallback(IObjMesh obj);
    
    public interface IObjMesh: IBVHElement {
        public void GenerateGeometry(RenderTarget target);
        public event MeshInvalidationCallback GeometryChanged;
    }
}
