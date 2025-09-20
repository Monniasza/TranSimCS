using System;
using System.Reflection;
using MonoGame.Extended.ECS;
using MonoGame.Extended.ECS.Systems;

namespace TranSimCS.Worlds.ECS {
    public static class ECSManager {
        private static Func<World, EntityManager> emGetter;
        private static Func<World, ComponentManager> cmGetter;
        private static Action<World, ISystem> addSystem;

        internal static bool Init() {
            if (emGetter != null && cmGetter != null && addSystem != null) return false;

            var ecsType = typeof(World);
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

            var ecsEMBinding = ecsType.GetProperty("EntityManager", bindingFlags);
            emGetter = ecsEMBinding.GetGetMethod(true).CreateDelegate<Func<World, EntityManager>>();

            var ecsCMBinding = ecsType.GetProperty("ComponentManager", bindingFlags);
            cmGetter = ecsCMBinding.GetGetMethod(true).CreateDelegate<Func<World, ComponentManager>>();

            var ecsSysBinding = ecsType.GetMethod("RegisterSystem", bindingFlags);
            addSystem = ecsSysBinding.CreateDelegate<Action<World, ISystem>>();

            return true;
        }

        public static EntityManager GetEntityManager(this World ecs) => emGetter(ecs);
        public static ComponentManager GetComponentManager(this World ecs) => cmGetter(ecs);
        public static void AddSystem(this World ecs, ISystem system) => addSystem(ecs, system);
    }
}
