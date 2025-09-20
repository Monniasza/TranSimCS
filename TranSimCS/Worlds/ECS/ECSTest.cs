using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended.ECS;

namespace TranSimCS.Worlds.ECS {
    internal class ECSTest {
        public ECSTest(TSWorld world) {
            var ecs = world.ECS;
            var em = ecs.GetEntityManager();
            var cm = ecs.GetComponentManager();

            Entity entity = ecs.CreateEntity();
            entity.Attach(new PositionComponent());
        }
    }
}
