using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Model;
using TranSimCS.Worlds;

namespace TranSimCS.SceneGraph {
    public sealed class SceneLeaf : SceneNode {
        public IObjMesh Obj { get; }
        public SceneProxy Proxy { get; }

        public SceneLeaf(IObjMesh obj) {
            Obj = obj;
            Proxy = new SceneProxy(this);
        }

        public override BoundingBox GetBounds() => Obj.GetBounds();
    }
}
