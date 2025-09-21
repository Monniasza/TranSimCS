using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arch.Core;

namespace TranSimCS.Worlds.ECS {
    public static class ECSUtils {
        public static readonly QueryDescription meshed = new QueryDescription().WithAll<MeshComponent>();

        public static readonly QueryDescription nodes = new QueryDescription().WithAll<NodeComponent, PositionComponent>();
        public static readonly QueryDescription nodesToRemesh = nodes.WithExclusive<MeshComponent>();        
    }
}
