using TranSimCS.Geometry;
using TranSimCS.Worlds;

namespace TranSimCS.SceneGraph {
    public sealed class SceneLeaf : SceneNode {
        public IObjMesh Obj { get; }
        public SceneProxy Proxy { get; }

        public SceneLeaf(IObjMesh obj) {
            Obj = obj;
            Proxy = new SceneProxy(this);
        }

        public override AABB GetBounds() => Obj.GetBounds();
    }
}
