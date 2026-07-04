using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;

namespace TranSimCS.Model {
    public static class MeshUnroll {
        /// <summary>
        /// Unrolls the mesh so all mesh instances become permanent meshes of the model
        /// </summary>
        /// <param name="mesh">mesh to unroll</param>
        public static void Unroll(this MultiMesh mesh) => Unroll(mesh, mesh);
        public static void Unroll(this MultiMesh src, MultiMesh dst) => Unroll(src, dst, TransformQ.Identity);
        public static void Unroll(this MultiMesh src, MultiMesh dst, TransformQ transform) {
            Transform3 tx = new Transform3(transform.ToMatrix());

            //Unroll each MeshInstance
            if (src != dst) foreach(var row in src.RenderBins) {
                var tex = row.Key;
                var mesh = row.Value;
                var newBin = dst.GetOrCreateRenderBinForced(tex);
                tx.TransformOutOfPlace(mesh, newBin);
            }

            foreach(var meshInstance in src.meshInstances) {
                var newTransform = transform * meshInstance.PositionRotation;
                Unroll(meshInstance.Mesh, dst, newTransform);
            }
        }
    }
}
