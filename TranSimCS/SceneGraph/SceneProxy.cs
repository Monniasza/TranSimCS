using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Model;
using TranSimCS.Worlds;

namespace TranSimCS.SceneGraph {
    public sealed class SceneProxy: IObjMesh{
        private readonly SceneLeaf owner;
        public SceneProxy(SceneLeaf owner) {
            this.owner = owner;
        }
        public event MeshInvalidationCallback GeometryChanged {
            add => owner.Obj.GeometryChanged += value;
            remove => owner.Obj.GeometryChanged -= value;
        }
        public BoundingBox GetBounds()  => owner.Obj.GetBounds();
        public bool ComputeIntersection(Ray ray, out float distance, out object? tag) => owner.Obj.ComputeIntersection(ray, out distance, out tag);
        public void GenerateGeometry(RenderTarget target) => owner.Obj.GenerateGeometry(target);
    }
}
