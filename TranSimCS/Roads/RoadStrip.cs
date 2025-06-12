using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using static TranSimCS.Roads.Roads;

namespace TranSimCS.Roads {
    public enum SegmentHalf {
        Start, // Represents the left half of a road segment
        End // Represents the right half of a road segment
    }

    public struct LaneRange {
        public RoadStrip road; // The road connection this tag is associated with
        public Lane startLaneIndexL; // The starting lane index for the tag
        public Lane startLaneIndexR;
        public Lane endLaneIndexL;
        public Lane endLaneIndexR;
        public LaneRange(RoadStrip road, Lane startLaneIndexL, Lane startLaneIndexR, Lane endLaneIndexL, Lane endLaneIndexR) {
            this.road = road;
            this.startLaneIndexL = startLaneIndexL;
            this.startLaneIndexR = startLaneIndexR;
            this.endLaneIndexL = endLaneIndexL;
            this.endLaneIndexR = endLaneIndexR;
        }
    }

    public class RoadStripEventArgs : EventArgs {
        public LaneStrip lane { get; } // The road strip associated with the event
        public RoadStripEventArgs(LaneStrip strip) {
            lane = strip; // Initialize the road strip associated with the event
        }
    }

    /// <summary>
    /// Represents a connection between two road nodes, including lane indices and specifications.
    /// </summary>
    /// <remarks>A <see cref="RoadStrip"/> defines the relationship between two road nodes, specifying
    /// the lanes involved at each node and their respective indices. It also includes properties for lane
    /// specifications and rendering-related data, such as meshes for visualization.</remarks>
    public class RoadStrip(RoadNode startNode, RoadNode endNode) {
        // Properties to hold the start and end nodes and their respective lane indices
        public readonly RoadNode StartNode = startNode; // The starting road node of the connection
        public readonly RoadNode EndNode = endNode;
        public RoadNode GetHalf(SegmentHalf selectedRoadHalf) {
            if (selectedRoadHalf == SegmentHalf.Start) {
                return StartNode; // Return the start node if the selected half is Start
            } else if (selectedRoadHalf == SegmentHalf.End) {
                return EndNode; // Return the end node if the selected half is End
            } else {
                throw new ArgumentException("Invalid segment half specified."); // Throw an exception for invalid segment half
            }
        }

        private List<LaneStrip> lanes = new(); // List of lane strips associated with this road connection
        public void AddLaneStrip(LaneStrip laneStrip) {
            if (lanes.Contains(laneStrip)) throw new ArgumentException("Lanes must not be duplicated");
            lanes.Add(laneStrip); // Add a new lane strip to the connection
            OnLaneAdded?.Invoke(this, new RoadStripEventArgs(laneStrip)); // Trigger the OnLaneAdded event
            InvalidateMesh(); // Invalidate the mesh for the lane strip to ensure it is regenerated
        }
        public void RemoveLaneStrip(LaneStrip laneStrip) {
            var removal = lanes.Remove(laneStrip); // Remove a lane strip from the connection
            Debug.Print($"Has the lane been removed? {removal}");
            OnLaneRemoved?.Invoke(this, new RoadStripEventArgs(laneStrip)); // Trigger the OnLaneRemoved event
            InvalidateMesh(); // Invalidate the mesh for the lane strip to ensure it is regenerated
        }
        public IReadOnlyCollection<LaneStrip> Lanes => lanes.AsReadOnly(); // Get the list of lane strips associated with this road connection
        public event EventHandler<RoadStripEventArgs>? OnLaneAdded; // Event triggered when lanes are added or removed
        public event EventHandler<RoadStripEventArgs>? OnLaneRemoved; // Event triggered when lanes are removed

        public LaneRange FullSizeTag() {
            int maxIdx = lanes.Count - 1; // Get the maximum index of the lanes
            return new LaneRange(this, lanes[0].StartLane, lanes[maxIdx].StartLane, lanes[0].EndLane, lanes[maxIdx].EndLane); // Create a LaneTag with the full size of the connection
        }

        //Meshes for the lane connection (can be used for rendering and cached)
        private Mesh _wholeNodeMesh; // Mesh for the lane connection at the end node
        public Mesh WholeNodeMesh { get { 
            if (_wholeNodeMesh != null) return _wholeNodeMesh; // If the end mesh is set, return it
            _wholeNodeMesh = new Mesh();
            RoadRenderer.GenerateRoadSegmentBoundingMesh(this, _wholeNodeMesh); // Otherwise, render the road segment
            return _wholeNodeMesh; // Return the rendered mesh
        } private set => _wholeNodeMesh = value; } // Mesh for the lane connection at the start node
        internal void InvalidateMesh() {
            _wholeNodeMesh = null; // Invalidate the mesh, forcing it to be re-rendered
            foreach(var lane in lanes) 
                lane.InvalidateMesh(); // Invalidate the mesh for each lane strip
        }
    }
}
