using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TranSimCS.Geometry;
using TranSimCS.Mode;
using TranSimCS.Property;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;
using static TranSimCS.Model.MeshUnroll;

namespace TranSimCS.TrafficLights {
    public sealed class TrafficLightGroup: Obj, IObjMesh, IDemolish {
        public record struct GeneratedNode(TrafficLight TrafficLight, MeshDrawInstance Model) {
            public GeneratedNode(HalfLane hl, TrafficLightGroup tlg, MeshDrawInstance mdi):
                this(new(hl, tlg), mdi){}
        }

        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        public static readonly TrafficLightPhase EmptyPhase = new TrafficLightPhase(1, ImmutableHashSet<HalfLane>.Empty);

        //Structural properties
        private readonly List<HalfLane> _controlledHalfLanes = [];
        public ReadOnlyCollection<HalfLane> ControlledHalfLanes => new(_controlledHalfLanes);
        internal void OnLaneAdded(HalfLane hlane) {
            _controlledHalfLanes.Add(hlane);
            FirePropertyEvent(this, new(PropertyNames.LanesOfTrafficLight));
        }
        internal void OnLaneRemoved(HalfLane hlane) {
            _controlledHalfLanes.Remove(hlane);
            if (_controlledHalfLanes.Count < 1) Demolish();
            FirePropertyEvent(this, new(PropertyNames.LanesOfTrafficLight));
        }

        //Simulation properties
        public float Time;
        public int PhaseId;
        public List<TrafficLightPhase> Phases { get; private set; } = [];

        //Generated properties
        public ImmutableArray<GeneratedNode> GeneratedGeometry;

        public event MeshInvalidationCallback GeometryChanged;
        public TrafficLightPhase CurrentPhase => (Phases.Count == 0) ? EmptyPhase : Phases[PhaseId];

        public TrafficLightGroup(Guid? guid = null): base(guid) {
            DependencyChanged += HandleDependencyChanged;
        }

        private void HandleDependencyChanged(Obj targetObject, Obj dependencyObject, string? propertyName) {
            GenerateInstances();
            GeometryChanged?.Invoke(this);
            computedBounds = default;
            foreach(var instance in GeneratedGeometry) {
                var instanceBounds = instance.Model.GetBounds();
                computedBounds = AABB.CreateMerged(computedBounds, instanceBounds);
            }
        }

        internal static void Update(TrafficLightGroup lights, float dt) {
            bool hasPhases = lights.Phases.Count > 0;

            //Advance the state
            int i = 0;
            const int max = 100;
            while (hasPhases && lights.Phases[lights.PhaseId].Duration < lights.Time){
                if(i == max) {
                    log.Warn("Excessive phase advancement. Maybe all phases are 0 seconds? " + lights.Guid);
                }
                var duration = lights.Phases[lights.PhaseId].Duration;
                if(!float.IsFinite(duration) || duration <= 0) {
                    log.Error($"The phase has a negative, zero, or invalid duration: {duration}. Deleting.");
                    lights.Phases.RemoveAt(lights.PhaseId);
                    if (lights.PhaseId >= lights.Phases.Count) lights.PhaseId = 0;
                    continue;
                }
                if(duration < 0.1) {
                    log.Warn("The phase has a very short duration");
                }
                lights.Time -= duration;
                lights.PhaseId++;
                if(lights.PhaseId >= lights.Phases.Count) lights.PhaseId = 0;
                i++;
            }

            if (!hasPhases) lights.PhaseId = 0;

            //Emit the state
            lights.GenerateInstances();
        }

        public bool IsGreen(HalfLane lane) {
            if (Phases.Count == 0) return true;
            return CurrentPhase.GreenLanes.Contains(lane);
        }

        private void GenerateInstances() {
            //Emit the state
            bool hasPhases = Phases.Count > 0;
            var currentPhase = hasPhases ? Phases[PhaseId] : EmptyPhase;
            int j = 0;
            const float height = 3.5f;
            var output = new GeneratedNode[ControlledHalfLanes.Count];
            foreach (var lane in ControlledHalfLanes) {
                bool isGreen = currentPhase.GreenLanes.Contains(lane);
                var model = isGreen ? TrafficLightMeshes.Green : TrafficLightMeshes.Red;
                var nodeTransform = lane.HalfNode.Cache.ReferenceFrame;
                nodeTransform.O += nodeTransform.X * lane.MiddlePosition;
                nodeTransform.O += nodeTransform.Y * height;
                nodeTransform = nodeTransform.Around();
                var transform = nodeTransform.ToQuaternion();
                var instance = new MeshDrawInstance(model, transform, TrafficLightMeshes.Texture, 12);
                output[j++] = new(lane, this, instance);
            }
            GeneratedGeometry = output.ToImmutableArray();
        }

        public bool ComputeIntersection(Ray3 ray, out float distance, out object? tag) {
            if (GeneratedGeometry == null) GenerateInstances();

            distance = float.PositiveInfinity;
            tag = null;
            foreach(var node in GeneratedGeometry) {
                var light = node.Model;
                bool intersects = light.ComputeIntersection(ray, out var distance0, out var tag0);
                if (intersects && distance0 < distance) {
                    distance = distance0;
                    tag = node.TrafficLight;
                }
            }
            return distance < float.MaxValue;
        }

        public void Demolish() {
            World?.TrafficLights.data.Remove(this);
        }

        public void GenerateGeometry(RenderTarget target) {
            foreach (var light in GeneratedGeometry) target(light.Model);
        }

        private AABB computedBounds;
        public AABB GetBounds() => computedBounds;
    }

    

    public struct TrafficLight: IDemolish, IEquatable<TrafficLight> {
        public HalfLane lane;
        public TrafficLightGroup TrafficLightGroup;

        public TrafficLight(HalfLane lane, TrafficLightGroup trafficLightGroup) {
            this.lane = lane;
            TrafficLightGroup = trafficLightGroup;
        }


        public void Demolish() => lane.TrafficLight = null;

        public override bool Equals(object? obj) {
            return obj is TrafficLight light && Equals(light);
        }

        public bool Equals(TrafficLight other) {
            return EqualityComparer<HalfLane>.Default.Equals(lane, other.lane);
        }

        public override int GetHashCode() {
            return HashCode.Combine(lane);
        }

        public static bool operator ==(TrafficLight left, TrafficLight right) {
            return left.Equals(right);
        }

        public static bool operator !=(TrafficLight left, TrafficLight right) {
            return !(left == right);
        }
    }

    public struct TrafficLightPhase : IEquatable<TrafficLightPhase> {
        public float Duration;
        public ImmutableHashSet<HalfLane> GreenLanes;

        public TrafficLightPhase(float duration, ImmutableHashSet<HalfLane> greenLanes) {
            Duration = duration;
            GreenLanes = greenLanes;
        }

        public override bool Equals(object? obj) {
            return obj is TrafficLightPhase phase && Equals(phase);
        }

        public bool Equals(TrafficLightPhase other) {
            return Duration == other.Duration &&
                   EqualityComparer<ImmutableHashSet<HalfLane>>.Default.Equals(GreenLanes, other.GreenLanes);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Duration, GreenLanes);
        }

        public static bool operator ==(TrafficLightPhase left, TrafficLightPhase right) {
            return left.Equals(right);
        }

        public static bool operator !=(TrafficLightPhase left, TrafficLightPhase right) {
            return !(left == right);
        }
    }
}
