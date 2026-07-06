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
        public IMeshSource MeshSource { get; }
        public Obj Obj { get; }

        public SceneProxy Proxy { get; }

        public SceneLeaf(IMeshSource meshSource, Obj obj) {
            MeshSource = meshSource;
            Obj = obj;

            Proxy = new SceneProxy(this);

            MeshSource.OnMeshInvalidated += () =>
            {
                // nothing structural here — tree handles it via proxy
            };
        }

        public override BoundingBox GetBounds()
            => MeshSource.GetBounds();
    }
}
