using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Cars;
using TranSimCS.Geometry;
using TranSimCS.Mode;
using TranSimCS.Model;
using TranSimCS.Property;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Spline;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Paths;

namespace TranSimCS.Roads.Strip {
    public class LaneStrip : IEquatable<LaneStrip?>, IDraggableObj, IRoadElement, IExtent, ILaneSpec, IDemolish {
        //ROAD ELEMENT
        public Guid Guid => Road.Guid;
        public Lane? GetLane() => null;
        public LaneStrip? GetLaneStrip() => this;
        public RoadNode? GetRoadNode() => null;
        public RoadStrip? GetRoadStrip() => Road;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => 0;
        public HalfLane? GetLaneEnd() => null;
        public RoadNodeEnd? GetNodeEnd() => null;
        public DualRange Bounds => Tag().ToDualRange();

        //Constant contents
        public HalfLane StartLane { get; private set; }
        public HalfLane EndLane { get; private set; }
        public RoadStrip Road { get; internal set; }

        public Property<LaneSpec> LaneSpecProp { get; }

        /// <summary>
        /// Specification of this <see cref="LaneStrip"/>, including properties like width, type, etc.
        /// </summary>
        public LaneSpec LaneSpec {
            get => LaneSpecProp.Value;
            set => LaneSpecProp.Value = value;
        }
        public LaneRange Tag() {
            var startRange = StartLane.Bounds;
            var endRange = EndLane.Bounds;
            if(StartLane.HalfNode == Road.EndNode) DataUtil.Swap(ref startRange, ref endRange);
            return new LaneRange(Road, startRange, endRange);
        }// Create a LaneTag for the lane strip, which includes the road and the start and end lanes

        public LaneStrip(HalfLane startLane, HalfLane endLane, LaneSpec? spec = null){
            _cache = new(this);
            StartLane = startLane;
            EndLane = endLane;
            LaneSpecProp = new(spec ?? LaneSpec.Default, "spec", null);
            LaneSpecProp.ValueChanged += (s, o, n) => {
                //The lane geometry changed, so the path this strip owns is no longer valid.
                if (_path != null) {
                    _path.Spec = n;
                    _path.MarkDirty();
                }
                Road?.FirePropertyEvent(Road, new(Guid + PropertyNames.NodeSpecSuffix));
            };
        }

        //Events
        internal void FireChanged() => Changed?.Invoke();
        public event Action? Changed;

        //Path ownership
        private SplinePath? _path;

        /// <summary>
        /// The <see cref="SplinePath"/> owned by this lane strip.
        /// <para>
        /// The path is created on first access and is anchored to this strip's two half lanes. It is
        /// owned by the strip: when the strip is removed from its road, the path is orphaned rather than
        /// deleted, so that traffic already on it can leave.
        /// </para>
        /// <para>
        /// Returns <see langword="null"/> when the strip is not part of a world yet, because a path
        /// cannot be registered without a world to register it in.
        /// </para>
        /// </summary>
        public SplinePath? Path {
            get {
                if (_path == null) CreatePath();
                return _path;
            }
        }

        /// <summary>
        /// Creates the path owned by this lane strip and registers it with the world.
        /// <para>
        /// Does nothing when the strip is not part of a world, or when it already owns a path.
        /// </para>
        /// </summary>
        private void CreatePath() => CreatePath(null);

        /// <summary>
        /// Creates the path owned by this lane strip and registers it with the world, using the given
        /// GUID.
        /// <para>
        /// Does nothing when the strip is not part of a world, or when it already owns a path.
        /// </para>
        /// </summary>
        /// <param name="guid">
        /// The GUID to give the path, or <see langword="null"/> to generate a new one. A saved GUID is
        /// passed here when loading a world, so that the same path is reused rather than a duplicate
        /// being created.
        /// </param>
        private void CreatePath(Guid? guid) {
            var world = Road?.World;
            if (world == null) return;
            if (_path != null) return;

            var claim = new LaneStripPathClaim(this);
            _path = new SplinePath(claim, guid);
            _path.Spec = LaneSpec;
            world.Paths.AddPath(_path);
        }

        /// <summary>
        /// Gets the path owned by this lane strip, creating it with the given GUID if it does not exist
        /// yet.
        /// <para>
        /// This is the entry point used when loading a world. The GUID has to be supplied at creation
        /// time because <see cref="Obj.Guid"/> is set-once, so a path that already exists keeps its own
        /// GUID and the supplied one is ignored.
        /// </para>
        /// </summary>
        /// <param name="guid">The GUID to give the path if it has to be created.</param>
        /// <returns>The existing or newly created path, or <see langword="null"/> when the strip is not
        /// part of a world.</returns>
        public SplinePath? GetOrCreatePath(Guid guid) {
            if (_path == null) CreatePath(guid);
            return _path;
        }

        /// <summary>
        /// Orphans the path owned by this lane strip.
        /// <para>
        /// Called when the strip is removed from its road. The path is not deleted: it becomes
        /// <see cref="PathState.Orphaned"/>, which keeps it resolvable by GUID and by direct reference so
        /// that traffic already on it can leave, while removing it from the spatial index so that no new
        /// traffic is routed onto it.
        /// </para>
        /// <para>
        /// This is safe to call more than once and safe to call when no path has been created.
        /// </para>
        /// </summary>
        internal void OrphanPath() {
            if (_path == null) return;
            if (_path.CurrentState != PathState.Active) return;
            _path.Displace(null);
        }

        /// <summary>
        /// The path owned by this lane strip, or <see langword="null"/> if none has been created.
        /// <para>
        /// Unlike <see cref="Path"/>, this does not create the path as a side effect.
        /// </para>
        /// </summary>
        public SplinePath? ExistingPath => _path;

        //Cache
        internal readonly LaneStripCache _cache;

        /// <summary>
        /// The centre line lookup table of this lane strip.
        /// <para>
        /// For a strip that has been removed from its road, the centre line cannot be regenerated, so
        /// the last generated lookup table is returned instead. This keeps the property non-null for
        /// callers that are still draining traffic off a deleted strip, and avoids a
        /// <see cref="NullReferenceException"/> when a segment is deleted from under a car.
        /// </para>
        /// </summary>
        public OrthodistantLUT SplineLUT => _cache.CenterLUT;
        public GridMesh<Vector3, RoadSplineComponent> AllStrips => _cache.AllStrips;
        public MultiMesh GetMesh() => _cache.Mesh;
        public ExtentIndex ExtentIndex => _cache.ExtentIndex;
        public bool IsAlive => Road != null;
        public bool IsDead => Road == null;

        //Car cache. Maintained by CarStack
        internal List<CarEntry> _carsOnStrip = [];
        public IReadOnlyList<CarEntry> CarsOnStrip => _carsOnStrip.AsReadOnly();
        internal void InsertCar(Car car) {

        }
        internal void RemoveCar(Car car) {
            for (int i = 0; i < _carsOnStrip.Count; i++) {
                var entry = _carsOnStrip[i];
                if(entry.car == car) {
                    _carsOnStrip.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Find the index of the first car ahead of <paramref name="position"/>, or <see cref="CarsOnStrip"/>.Count, if not found
        /// </summary>
        public int FindFirstAheadIndex(float position) {
            int min = 0;
            int max = _carsOnStrip.Count;

            while (min < max) {
                int mid = (min + max) >> 1;
                if (_carsOnStrip[mid].positionOnStrip <= position)
                    min = mid + 1;
                else
                    max = mid;
            }
            return min;
        }
        /// <summary>
        /// Find the index of the last car behind <paramref name="position"/>, or -1 if not found
        /// </summary>
        public int FindLastBehindIndex(float position) {
            int min = 0;
            int max = _carsOnStrip.Count;

            while (min < max) {
                int mid = (min + max) >> 1;

                if (_carsOnStrip[mid].positionOnStrip < position)
                    min = mid + 1;
                else
                    max = mid;
            }

            return min - 1;
        }

        public void InvalidateMesh() {
            _cache.Invalidate();// Invalidate the cached mesh, forcing it to be regenerated next time
        }

        public HalfLane GetHalf(SegmentHalf selectedRoadHalf) {
            if(selectedRoadHalf == SegmentHalf.Start) {
                return StartLane; // Return the starting lane if the selected half is Start
            } else if (selectedRoadHalf == SegmentHalf.End) {
                return EndLane; // Return the ending lane if the selected half is End
            } else {
                throw new ArgumentException("Invalid segment half specified."); // Throw an exception for invalid segment half
            }
        }

        public void Destroy() {
            OrphanPath();
            Road?.RemoveLaneStrip(this);
            InvalidateMesh();
        }

        //Dragging
        IPosition[] IDraggableObj.DraggableComponents() => [StartLane.HalfNode, EndLane.HalfNode];

        public override bool Equals(object? obj) {
            return Equals(obj as LaneStrip);
        }

        public bool Equals(LaneStrip? other) {
            return other is not null &&
                   EqualityComparer<HalfLane>.Default.Equals(StartLane, other.StartLane) &&
                   EqualityComparer<HalfLane>.Default.Equals(EndLane, other.EndLane) &&
                   EqualityComparer<RoadStrip>.Default.Equals(Road, other.Road) &&
                   EqualityComparer<LaneSpec>.Default.Equals(LaneSpec, other.LaneSpec);
        }

        public override int GetHashCode() {
            return HashCode.Combine(StartLane, EndLane, Road, LaneSpec);
        }

        public bool IsBetween(HalfLane start, HalfLane end) {
            return start == StartLane && end == EndLane || start == EndLane && end == StartLane;
        }

        public bool IsConnected(HalfLane end) {
            return end == StartLane || end == EndLane;
        }
        public SegmentHalf? WhichEnd(HalfLane end) {
            if (end == StartLane) return SegmentHalf.Start;
            if (end == EndLane) return SegmentHalf.End;
            return null;
        }

        public static bool operator ==(LaneStrip? left, LaneStrip? right) {
            return EqualityComparer<LaneStrip>.Default.Equals(left, right);
        }

        public static bool operator !=(LaneStrip? left, LaneStrip? right) {
            return !(left == right);
        }

        /// <summary>
        /// Replaces this lane strip with a new lane strip in the opposite direction.
        /// If this lane strip was added to a road strip, it mutates the road strip.
        /// </summary>
        /// <returns>a new lane strip in reverse direction</returns>
        public LaneStrip ReverseDirection() {
            var road = Road;
            var newStart = EndLane;
            var newEnd = StartLane;
            var newSpec = LaneSpec.Reverse();

            LaneStrip newLaneStrip = new LaneStrip(newStart, newEnd, newSpec);
            road?.AddLaneStrip(newLaneStrip);
            road?.RemoveLaneStrip(this);
            return newLaneStrip;
        }

        public bool IsReverse() => StartLane.HalfNode == Road?.EndNode && EndLane != StartLane;

        public void Demolish() {
            OrphanPath();
            Road.RemoveLaneStrip(this);
        }
    }
}
