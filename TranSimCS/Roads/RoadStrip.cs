using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.SceneGraph;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Property;
using static TranSimCS.Roads.Roads;

namespace TranSimCS.Roads {
    public enum SegmentHalf {
        Start, // Represents the left half of a road segment
        End // Represents the right half of a road segment
    }

    public struct LaneRange(RoadStrip road, Lane startLaneIndexL, Lane startLaneIndexR, NodeEnd startSide, Lane endLaneIndexL, Lane endLaneIndexR, NodeEnd endSide) {
        public RoadStrip road = road; // The road connection this tag is associated with
        public Lane startLaneIndexL = startLaneIndexL; // The starting lane index for the tag
        public Lane startLaneIndexR = startLaneIndexR;
        public NodeEnd startSide = startSide;
        public Lane endLaneIndexL = endLaneIndexL;
        public Lane endLaneIndexR = endLaneIndexR;
        public NodeEnd endSide = endSide;
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
    public class RoadStrip: Obj, IObjMesh<RoadStrip> {
        // Properties to hold the start and end nodes and their respective lane indices
        public readonly RoadNodeEnd StartNode; // The starting road node of the connection
        public readonly RoadNodeEnd EndNode;

        public readonly Property<RoadFinish> FinishProperty;
        public RoadFinish Finish { get => FinishProperty.Value; set => FinishProperty.Value = value; }

        public RoadStrip(RoadNodeEnd startNode, RoadNodeEnd endNode) {
            StartNode = startNode;
            EndNode = endNode;
            FinishProperty = new(RoadFinish.Embankment, "finish", this);
            Mesh = new MeshGenerator<RoadStrip>(this, GenerateMesh);
            Mesh.OnMeshInvalidated += () => InvalidateMesh0(this);
            PropertyChanged += RoadStrip_PropertyChanged;
        }

        private void RoadStrip_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
            Mesh.Invalidate();
        }

        public RoadNodeEnd GetHalf(SegmentHalf selectedRoadHalf) {
            if (selectedRoadHalf == SegmentHalf.Start) {
                return StartNode; // Return the start node if the selected half is Start
            } else if (selectedRoadHalf == SegmentHalf.End) {
                return EndNode; // Return the end node if the selected half is End
            } else {
                throw new ArgumentException("Invalid segment half specified."); // Throw an exception for invalid segment half
            }
        }
        public bool CheckEnds(RoadNodeEnd first, RoadNodeEnd second) {
            return (first == StartNode && second == EndNode) || (first == EndNode && second == StartNode);
        }

        private List<LaneStrip> lanes = new(); // List of lane strips associated with this road connection
        public void AddLaneStrip(LaneStrip laneStrip) {
            if(!MaybeAddLaneStrip(laneStrip)) throw new ArgumentException("Lanes must not be duplicated");
        }
        public bool RemoveLaneStrip(LaneStrip laneStrip) {
            var removal = lanes.Remove(laneStrip); // Remove a lane strip from the connection
            if(!removal) return false;
            laneStrip.road = null;
            OnLaneRemoved?.Invoke(this, new RoadStripEventArgs(laneStrip)); // Trigger the OnLaneRemoved event
            Mesh.Invalidate(); // Invalidate the mesh for the lane strip to ensure it is regenerated
            return true;
        }
        public bool MaybeAddLaneStrip(LaneStrip laneStrip) {
            if (lanes.Contains(laneStrip)) return false;
            lanes.Add(laneStrip); // Add a new lane strip to the connection
            laneStrip.road = this;
            OnLaneAdded?.Invoke(this, new RoadStripEventArgs(laneStrip)); // Trigger the OnLaneAdded event
            Mesh.Invalidate(); // Invalidate the mesh for the lane strip to ensure it is regenerated
            return true;
        }
        public IReadOnlyCollection<LaneStrip> Lanes => lanes.AsReadOnly(); // Get the list of lane strips associated with this road connection

        public event EventHandler<RoadStripEventArgs>? OnLaneAdded; // Event triggered when lanes are added or removed
        public event EventHandler<RoadStripEventArgs>? OnLaneRemoved; // Event triggered when lanes are removed

        public LaneRange? FullSizeTag() {
            int maxIdx = lanes.Count - 1; // Get the maximum index of the lanes

            int ls = int.MaxValue, rs = int.MinValue, le = int.MaxValue, re = int.MinValue;
            foreach (LaneStrip laneStrip in lanes) {
                var startIdx = laneStrip.StartLane.lane.Index;
                var endIdx = laneStrip.EndLane.lane.Index;

                if (laneStrip.StartLane.RoadNodeEnd == EndNode)
                    (startIdx, endIdx) = (endIdx, startIdx);

                if (startIdx < ls) ls = startIdx;
                if(startIdx > rs) rs = startIdx;
                if(endIdx < le) le = endIdx;
                if(endIdx > re) re = endIdx;
            }

            if (ls > rs || le > re) {
                Debug.Print("Empty lane strip");
                return null;
            }
            return new LaneRange(this,
                StartNode.GetLaneEnd(ls).lane, StartNode.GetLaneEnd(rs).lane, StartNode.End,
                EndNode.GetLaneEnd(le).lane, EndNode.GetLaneEnd(re).lane, EndNode.End);
        }

        //Meshes for the lane connection (can be used for rendering and cached)
        public MeshGenerator<RoadStrip> Mesh { get; init; }
        protected static void InvalidateMesh0(RoadStrip strip) {
            foreach (var lane in strip.lanes)
                lane.InvalidateMesh(); // Invalidate the mesh for each lane strip
        }

        /// <summary>
        /// Left start, right start, left end, right end
        /// </summary>
        public RoadBounds Bounds { get; private set; } 
        protected static void GenerateMesh(RoadStrip segment, MultiMesh mesh) {
            var bounds = new RoadBounds();
            //Generate bounds
            foreach (var lane in segment.Lanes) {
                var startLane = lane.StartLane;
                var endLane = lane.EndLane;
                if(startLane.RoadNodeEnd == segment.EndNode & endLane.RoadNodeEnd == segment.StartNode && startLane.RoadNodeEnd != endLane.RoadNodeEnd) {
                    (startLane, endLane) = (endLane, startLane);
                }

                bounds = bounds
                    .Update(startLane.lane.LeftPosition, endLane.lane.LeftPosition)
                    .Update(startLane.lane.RightPosition, endLane.lane.RightPosition);
            }
            if(segment.StartNode.End == NodeEnd.Backward) {
                (bounds.leftStart, bounds.rightStart) = (-bounds.rightStart, -bounds.leftStart);
            }
            if (segment.EndNode.End == NodeEnd.Forward) {
                (bounds.leftEnd, bounds.rightEnd) = (-bounds.rightEnd, -bounds.leftEnd);
            }
            segment.Bounds = bounds;
            SegmentRenderer.GenerateRoadSegmentFullMesh(segment, mesh); // Otherwise, render the road segment
        }

        public SplineFrame CalcSplineFrame() {
            var start = StartNode.CalcReferenceFrame();
            var end = EndNode.CalcReferenceFrame();

            var zeroSpline = GeometryUtils.GenerateJoinSpline(start.O, end.O, start.Z, end.Z);
            var xSpline = GeometryUtils.GenerateJoinSpline(start.O + start.X, end.O - end.X, start.Z, end.Z);
            var ySpline = GeometryUtils.GenerateJoinSpline(start.O + start.Y, end.O + end.Y, start.Z, end.Z);

            return new SplineFrame(zeroSpline, xSpline - zeroSpline, ySpline - zeroSpline, new Spline.Bezier3());
        }
    }
}
