using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;
using static TranSimCS.Model.MeshUnroll;

namespace TranSimCS.Worlds {
    public delegate void RenderTarget(MeshDrawInstance geometry);
    public static class RenderTargetMethods {
        public static void Draw(this RenderTarget target, MeshDrawInstance geometry) => target(geometry);
        public static void Draw(this RenderTarget target, MultiMesh mesh) {
            foreach (var mdi in MeshUnroll.MeshTraversal.Traverse(mesh)) target(mdi);
        }
        public static void Draw(this RenderTarget target, MeshInstance mesh) {
            foreach (var mdi in MeshUnroll.MeshTraversal.Traverse(mesh.Mesh, mesh.PositionRotation)) target(mdi);
        }
    }
}
