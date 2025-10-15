using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Model;
using TranSimCS.Worlds;

namespace TranSimCS.SceneGraph {
    public class SceneLeaf<T>: SceneNode where T: Obj, IObjMesh<T> {

        public readonly MeshGenerator<T> meshGenerator;

        public SceneLeaf(MeshGenerator<T> meshGenerator) {
            this.meshGenerator = meshGenerator;
            meshGenerator.OnRemoveMesh += MeshGenerator_OnRemoveMesh;
        }

        private void MeshGenerator_OnRemoveMesh(T obj) {
            Invalidate();
        }

        protected override BoundingBox CalcBounds() {
            return meshGenerator.GetMesh().RenderBins.Values.Select(x => MeshUtil.BoundingBox(x)).Aggregate(BoundingBox.CreateMerged);
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
