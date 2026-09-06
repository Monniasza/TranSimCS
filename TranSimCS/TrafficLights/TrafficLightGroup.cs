using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Property;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;

namespace TranSimCS.TrafficLights {
    public sealed class TrafficLightGroup: Obj, IObjMesh {
        //Simulation properties
        public float Time;
        public int CurrentPhase;
        public List<TrafficLightPhase> Phases { get; private set; } = [];

        public event MeshInvalidationCallback GeometryChanged;

        internal static void Update(double dt, TrafficLightGroup lights) {

        }

        public bool ComputeIntersection(Ray3 ray, out float distance, out object? tag) {
            throw new NotImplementedException();
        }

        public void GenerateGeometry(RenderTarget target) {
            throw new NotImplementedException();
        }

        public AABB GetBounds() {
            throw new NotImplementedException();
        }
    }

    public struct TrafficLightPhase {
        float Duration;
        ImmutableHashSet<Lane> GreenLanes;
    }
}
