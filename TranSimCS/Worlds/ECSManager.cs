using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended.ECS;
using MonoGame.Extended.ECS.Systems;

namespace TranSimCS.Worlds {
    public static class ECSManager {
        private static Func<World, EntityManager> emGetter;
        private static Func<World, ComponentManager> cmGetter;
        private static Action<World, ISystem> addSystem;

        internal static void Init() {
            var ecsType = typeof(World);
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

            var ecsEMBinding = ecsType.GetProperty("EntityManager", bindingFlags);
            emGetter = ecsEMBinding.GetGetMethod(true).CreateDelegate<Func<World, EntityManager>>();

            var ecsCMBinding = ecsType.GetProperty("ComponentManager", bindingFlags);
            cmGetter = ecsEMBinding.GetGetMethod(true).CreateDelegate<Func<World, ComponentManager>>();

            var ecsSysBinding = ecsType.GetMethod("RegisterSystem", bindingFlags);
            addSystem = ecsSysBinding.CreateDelegate<Action<World, ISystem>>();

        }

        public static EntityManager GetEntityManager(this World ecs) => emGetter(ecs);
        public static ComponentManager GetComponentManager(this World ecs) => cmGetter(ecs);
        public static void AddSystem(this World ecs, ISystem system) => addSystem(ecs, system);
    }
}
