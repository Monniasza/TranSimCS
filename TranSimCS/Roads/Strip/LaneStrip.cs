using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Model;
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
        public void Drag(Vector3 vector, Vector3 dragFrom) => laneEnd.Drag(vector, dragFrom);
        public void Rotate(int fieldAzimuth, float pitch, float tilt) => laneEnd.Rotate(fieldAzimuth, pitch, tilt);

        //ROAD ELEMENT
        public Guid Guid => strip.road.Guid;
        public Lane? GetLane() => strip.GetHalf(half).lane;
        public LaneStrip? GetLaneStrip() => strip;
        public RoadNode? GetRoadNode() => strip.GetHalf(half).RoadNodeEnd.Node;
        public RoadStrip? GetRoadStrip() => strip.road;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => half.Discriminant();
        public LaneEnd? GetLaneEnd() => laneEnd;
        public RoadNodeEnd? GetNodeEnd() => null;
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

        private LaneEnd startLane;
        private LaneEnd endLane;
        public RoadStrip road { get; internal set;}


        public LaneSpec _spec; // Specification of the lane strip, including properties like width, type, etc.

        public LaneSpec Spec {
            get {
                var width = 0f;
                var n = 0;
                if(startLane.lane != null) {
                    width += startLane.lane.Width;
                    n++;
                }
                if(endLane.lane != null) {
                    width += endLane.lane.Width;
                    n++;
                }
                if(n != 0) _spec.Width = width / n;
                return _spec;
            }
            set {
                if (_spec == value) return;
                InvalidateMesh();
                _spec = value;
            }
        }
        public LaneEnd StartLane {
            get => startLane;
            set {
                var old = startLane;
                old.lane?.connections.Remove(this); // Remove the current lane strip from the old starting lane's connections
                value.lane?.connections.Add(this); // Add the lane strip to the new starting lane's connections
                InvalidateMesh();
                startLane = value;
            }
        }
        public LaneEnd EndLane {
            get => endLane;
            set {
                var old = endLane;
                old.lane?.connections.Remove(this); // Remove the current lane strip from the old starting lane's connections
                value.lane?.connections.Add(this); // Add the lane strip to the new starting lane's connections
                InvalidateMesh();
                endLane = value;
            }
        }

        public LaneRange Tag => new LaneRange(road, StartLane.lane, StartLane.lane, StartLane.end, EndLane.lane, EndLane.lane, EndLane.end); // Create a LaneTag for the lane strip, which includes the road and the start and end lanes

        public LaneStrip(LaneEnd startLane, LaneEnd endLane) {
            StartLane = startLane!; // Starting lane of the lane strip
            EndLane = endLane!; // Ending lane of the lane strip
            Spec = LaneSpec.Default; // Default specification for the lane strip
        }

        public LaneStrip(LaneEnd start, LaneEnd end, LaneSpec spec) {
            StartLane = start;
            EndLane = end;
            Spec = spec;
        }

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
            var startEnd = startLane.end;
            var endEnd = endLane.end;
            StartLane = new LaneEnd(startEnd, null);
            EndLane = new LaneEnd(endEnd, null);
            currentRoad?.RemoveLaneStrip(this);
        }

        //Dragging
        void IDraggableObj.Drag(Vector3 vector, Vector3 dragFrom) {
            StartLane.Drag(vector, dragFrom);
            EndLane.Drag(vector, dragFrom);
        }


        public override bool Equals(object? obj) {
            return Equals(obj as LaneStrip);
        }

        public bool Equals(LaneStrip? other) {
            return other is not null &&
                   EqualityComparer<LaneEnd>.Default.Equals(startLane, other.startLane) &&
                   EqualityComparer<LaneEnd>.Default.Equals(endLane, other.endLane) &&
                   EqualityComparer<RoadStrip>.Default.Equals(road, other.road) &&
                   EqualityComparer<LaneSpec>.Default.Equals(Spec, other.Spec);
        }

        public override int GetHashCode() {
            return HashCode.Combine(startLane, endLane, road, Spec);
        }

        public bool IsBetween(LaneEnd start, LaneEnd end) {
            return start == StartLane && end == EndLane || start == EndLane && end == StartLane;
        }

        public void Rotate(int fieldAzimuth, float pitch, float tilt) {
            //unused
        }

        public static bool operator ==(LaneStrip? left, LaneStrip? right) {
            return EqualityComparer<LaneStrip>.Default.Equals(left, right);
        }

        public static bool operator !=(LaneStrip? left, LaneStrip? right) {
            return !(left == right);
        }
    }
}
