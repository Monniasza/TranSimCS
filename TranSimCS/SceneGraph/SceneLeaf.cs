using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Model;
using TranSimCS.Worlds;

namespace TranSimCS.SceneGraph {
    public class SceneLeaf: SceneNode{

        public readonly IMeshSource meshGenerator;
        public readonly Obj obj;

        public SceneLeaf(IMeshSource meshGenerator, Obj obj) {
            ArgumentNullException.ThrowIfNull(meshGenerator, nameof(meshGenerator));

            this.meshGenerator = meshGenerator;
            meshGenerator.OnMeshInvalidated += MeshGenerator_OnRemoveMesh;
            this.obj = obj;
        }

        private void MeshGenerator_OnRemoveMesh() {
            Invalidate();
        }

        protected override BoundingBox CalcBounds() => meshGenerator.GetMesh().GetBounds();

        protected override bool FindInternal(Ray ray, out SceneNode? node, out float dist, out object? tag) {
            var isIntersecting = meshGenerator.GetMesh().ComputeIntersection(ray, out dist, out tag);
            if (isIntersecting){
                node = this;
                return true;
            } else {
                return Reject(this, out node, out dist, out tag);
            }
        }
    }
}
