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
            LaneSpecProp.ValueChanged += (s, o, n) => Road?.FirePropertyEvent(Road, new(Guid + PropertyNames.NodeSpecSuffix));
        }

        //Cache
        internal readonly LaneStripCache _cache;
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

        public void Demolish() => Road.RemoveLaneStrip(this);
    }
}
