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

        public SceneLeaf(IMeshSource meshGenerator) {
            this.meshGenerator = meshGenerator;
            meshGenerator.OnMeshInvalidated += MeshGenerator_OnRemoveMesh;
        }

        private void MeshGenerator_OnRemoveMesh() {
            Invalidate();
        }

        protected override BoundingBox CalcBounds() {
            return SceneNode.FromMany(meshGenerator.GetMesh().RenderBins.Values);
        }

        protected override bool FindInternal(Ray ray, out SceneNode? node, out float dist, out object? tag) {
            var newTag = MeshUtil.RayIntersectMeshes(meshGenerator.GetMesh().RenderBins.Values, ray, out var intersectionDistance);
            if (intersectionDistance < float.MaxValue){
                node = this;
                dist = intersectionDistance;
                tag = newTag;
                return true;
            } else {
                return Reject(this, out node, out dist, out tag);
            }
        }
    }
}
