using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Property;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Roads.StripData;
using TranSimCS.Spline;
using TranSimCS.Worlds;
using static TranSimCS.Roads.StripData.RoadDataBuilder;

namespace TranSimCS.Roads.Strip {
    public class LaneStrip : IEquatable<LaneStrip?>, IDraggableObj, IRoadElement {
        //ROAD ELEMENT
        public Guid Guid => Road.Guid;
        public Lane? GetLane() => null;
        public LaneStrip? GetLaneStrip() => this;
        public RoadNode? GetRoadNode() => null;
        public RoadStrip? GetRoadStrip() => Road;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => 0;
        public LaneEnd? GetLaneEnd() => null;
        public RoadNodeEnd? GetNodeEnd() => null;

        //Constant contents
        public HalfLane StartLane { get; private set; }
        public HalfLane EndLane { get; private set; }
        public RoadStrip Road { get; internal set; }
        private LaneSpec _spec;
        /// <summary>
        /// Specification of this <see cref="LaneStrip"/>, including properties like width, type, etc.
        /// </summary>
        public LaneSpec Spec {
            get {
                var width = 0f;
                var n = 0;
                if(StartLane.Lane != null) {
                    width += StartLane.Lane.Width;
                    n++;
                }
                if(EndLane.Lane != null) {
                    width += EndLane.Lane.Width;
                    n++;
                }
                if(n != 0) _spec.Width = width / n;
                return _spec;
            }
            set {
                if (_spec == value) return;
                Road?.FirePropertyEvent(Road, new(Guid + PropertyNames.NodeSpecSuffix));
                _spec = value;
            }
        }

        public LaneConnectionData LaneConnectionData => new(Spec, StartLane.LaneNode, EndLane.LaneNode, this);
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
            Spec = spec ?? LaneSpec.Default;
        }

        //Cache
        private readonly LaneStripCache _cache;
        public OrthodistantLUT SplineLUT => _cache.CenterLUT;
        public GridMesh<Vector3, RoadSplineComponent> AllStrips => _cache.AllStrips;
        public LaneStripData LaneStripData => _cache.LaneStripData;
        public MultiMesh GetMesh() => _cache.Mesh;

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
                   EqualityComparer<LaneSpec>.Default.Equals(Spec, other.Spec);
        }

        public override int GetHashCode() {
            return HashCode.Combine(StartLane, EndLane, Road, Spec);
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
            var newSpec = Spec.Reverse();

            LaneStrip newLaneStrip = new LaneStrip(newStart, newEnd, newSpec);
            road?.AddLaneStrip(newLaneStrip);
            road?.RemoveLaneStrip(this);
            return newLaneStrip;
        }

        public bool IsReverse() => StartLane.HalfNode == Road.EndNode && EndLane != StartLane;
    }
}
