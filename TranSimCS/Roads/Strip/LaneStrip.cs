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
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Strip {
    public struct LaneStripEnd(LaneStrip strip, SegmentHalf half) : IDraggableObj, IRoadElement {
        public LaneStrip strip = strip;
        public SegmentHalf half = half;
        public LaneEnd laneEnd => strip.GetHalf(half);

        //DRAGGING
        IPosition[] IDraggableObj.DraggableComponents() => ((IDraggableObj)strip).DraggableComponents();

        //ROAD ELEMENT
        public Guid Guid => strip.road.Guid;
        public Lane? GetLane() => strip.GetHalf(half).lane;
        public LaneStrip? GetLaneStrip() => strip;
        public RoadNode? GetRoadNode() => strip.GetHalf(half).RoadNodeEnd.Node;
        public RoadStrip? GetRoadStrip() => strip.road;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => half.Discriminant();
        public LaneEnd? GetLaneEnd() => laneEnd;
        public RoadNodeEnd? GetNodeEnd() => strip.GetHalf(half).RoadNodeEnd;

        public IPosition[] DraggableComponents() => ((IDraggableObj)strip).DraggableComponents();
    }

    public class LaneStrip : IEquatable<LaneStrip?>, IDraggableObj, IRoadElement {
        //ROAD ELEMENT
        public Guid Guid => road.Guid;
        public Lane? GetLane() => null;
        public LaneStrip? GetLaneStrip() => this;
        public RoadNode? GetRoadNode() => null;
        public RoadStrip? GetRoadStrip() => road;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => 0;
        public LaneEnd? GetLaneEnd() => null;
        public RoadNodeEnd? GetNodeEnd() => null;

        //Constant contents
        public LaneEnd StartLane { get; private set; }
        public LaneEnd EndLane { get; private set; }
        public RoadStrip road { get; internal set; }
        private LaneSpec _spec;
        /// <summary>
        /// Specification of this <see cref="LaneStrip"/>, including properties like width, type, etc.
        /// </summary>
        public LaneSpec Spec {
            get {
                var width = 0f;
                var n = 0;
                if(StartLane.lane != null) {
                    width += StartLane.lane.Width;
                    n++;
                }
                if(EndLane.lane != null) {
                    width += EndLane.lane.Width;
                    n++;
                }
                if(n != 0) _spec.Width = width / n;
                return _spec;
            }
            set {
                if (_spec == value) return;
                road?.FirePropertyEvent(road, new(Guid + PropertyNames.NodeSpecSuffix));
                _spec = value;
            }
        }
        

        public LaneRange Tag() {
            var startRange = StartLane.Range();
            var endRange = EndLane.Range();
            if(StartLane.RoadNodeEnd == road.EndNode) DataUtil.Swap(ref startRange, ref endRange);
            return new LaneRange(road, startRange, endRange);
        }// Create a LaneTag for the lane strip, which includes the road and the start and end lanes

        public LaneStrip(LaneEnd startLane, LaneEnd endLane, LaneSpec? spec = null){
            _cache = new(this);
            StartLane = startLane;
            EndLane = endLane;
            Spec = spec ?? LaneSpec.Default;
        }

        //Spline cache
        private readonly LaneStripCache _cache;
        public RoadSplineComponent SplineCache => _cache.AsphaltCache;
        public RoadSplineComponent DrivableAreaStrip => _cache.DrivableAreaCache;
        public SplineLUT SplineLUT => _cache.CenterLUT;
        public SplineLUT LateralLUT => _cache.LateralLUT;
        public ImmutableArray<RoadSplineComponent> Lines => _cache.Lines;
        public ImmutableArray<RoadSplineComponent> AllStrips => _cache.AllStrips;

        //Mesh cache
        private MultiMesh? mesh; // Cached mesh for the lane strip
        public MultiMesh GetMesh() {
            if (mesh == null) {
                mesh = new MultiMesh(); // Create a new mesh if it doesn't exist
                StripRenderer.GenerateLaneStripMesh(this, mesh); // Generate the mesh for the lane strip if it doesn't exist
            }
            return mesh; // Return the cached mesh
        }
        public void InvalidateMesh() {
            mesh = null; // Invalidate the cached mesh, forcing it to be regenerated next time
            _cache.Invalidate();
        }

        public LaneEnd GetHalf(SegmentHalf selectedRoadHalf) {
            if(selectedRoadHalf == SegmentHalf.Start) {
                return StartLane; // Return the starting lane if the selected half is Start
            } else if (selectedRoadHalf == SegmentHalf.End) {
                return EndLane; // Return the ending lane if the selected half is End
            } else {
                throw new ArgumentException("Invalid segment half specified."); // Throw an exception for invalid segment half
            }
        }

        public void Destroy() {
            var currentRoad = road;
            var startEnd = StartLane.end;
            var endEnd = EndLane.end;
            StartLane = new LaneEnd(startEnd, null);
            EndLane = new LaneEnd(endEnd, null);
            currentRoad?.RemoveLaneStrip(this);
            InvalidateMesh();
        }

        //Dragging
        IPosition[] IDraggableObj.DraggableComponents() => [StartLane.RoadNodeEnd, EndLane.RoadNodeEnd];

        public override bool Equals(object? obj) {
            return Equals(obj as LaneStrip);
        }

        public bool Equals(LaneStrip? other) {
            return other is not null &&
                   EqualityComparer<LaneEnd>.Default.Equals(StartLane, other.StartLane) &&
                   EqualityComparer<LaneEnd>.Default.Equals(EndLane, other.EndLane) &&
                   EqualityComparer<RoadStrip>.Default.Equals(road, other.road) &&
                   EqualityComparer<LaneSpec>.Default.Equals(Spec, other.Spec);
        }

        public override int GetHashCode() {
            return HashCode.Combine(StartLane, EndLane, road, Spec);
        }

        public bool IsBetween(LaneEnd start, LaneEnd end) {
            return start == StartLane && end == EndLane || start == EndLane && end == StartLane;
        }

        public bool IsConnected(LaneEnd end) {
            return end == StartLane || end == EndLane;
        }
        public SegmentHalf? WhichEnd(LaneEnd end) {
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
            var newStart = EndLane;
            var newEnd = StartLane;
            var newSpec = Spec.Reverse();
            LaneStrip newLaneStrip = new LaneStrip(newStart, newEnd, newSpec);
            road?.RemoveLaneStrip(this);
            road?.AddLaneStrip(newLaneStrip);
            return newLaneStrip;
        }

        public bool IsReverse() => StartLane.RoadNodeEnd == road.EndNode && EndLane != StartLane;
    }
}
