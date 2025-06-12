using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Roads {
    public class LaneStrip {
        private Lane startLane;
        private Lane endLane;
        public readonly RoadStrip road;
        public LaneSpec spec; // Specification of the lane strip, including properties like width, type, etc.

        public Lane StartLane {
            get => startLane;
            set {
                var old = startLane;
                old?.connections.Remove(this); // Remove the current lane strip from the old starting lane's connections
                value?.connections.Add(this); // Add the lane strip to the new starting lane's connections
                InvalidateMesh();
                startLane = value;
            }
        }
        public Lane EndLane {
            get => endLane;
            set {
                var old = endLane;
                old?.connections.Remove(this); // Remove the current lane strip from the old starting lane's connections
                value?.connections.Add(this); // Add the lane strip to the new starting lane's connections
                InvalidateMesh();
                endLane = value;
            }
        }

        public LaneRange Tag => new LaneRange(road, StartLane, StartLane, EndLane, EndLane); // Create a LaneTag for the lane strip, which includes the road and the start and end lanes

        public LaneStrip(RoadStrip road, Lane startLane, Lane endLane) {
            this.road = road; // Reference to the road this lane strip belongs to
            this.StartLane = startLane; // Starting lane of the lane strip
            this.EndLane = endLane; // Ending lane of the lane strip
            this.spec = LaneSpec.Default; // Default specification for the lane strip
        }

        //Mesh cache
        private Mesh? mesh; // Cached mesh for the lane strip
        public Mesh GetMesh() {
            if (mesh == null) {
                mesh = new Mesh(); // Create a new mesh if it doesn't exist
                RoadRenderer.GenerateLaneStripMesh(this, mesh); // Generate the mesh for the lane strip if it doesn't exist
            }
            return mesh; // Return the cached mesh
        }
        public void InvalidateMesh() {
            mesh = null; // Invalidate the cached mesh, forcing it to be regenerated next time
        }

        public Lane GetHalf(SegmentHalf selectedRoadHalf) {
            if(selectedRoadHalf == SegmentHalf.Start) {
                return StartLane; // Return the starting lane if the selected half is Start
            } else if (selectedRoadHalf == SegmentHalf.End) {
                return EndLane; // Return the ending lane if the selected half is End
            } else {
                throw new ArgumentException("Invalid segment half specified."); // Throw an exception for invalid segment half
            }
        }

        public void Destroy() {
            StartLane = null;
            EndLane = null;
            road.RemoveLaneStrip(this);
        }
    }
}
