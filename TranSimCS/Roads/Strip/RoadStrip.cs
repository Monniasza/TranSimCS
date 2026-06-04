using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using NLog;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Property;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.StripGenerator;
using TranSimCS.SceneGraph;
using TranSimCS.Spline;
using TranSimCS.Worlds;
using static TranSimCS.Roads.Roads;

namespace TranSimCS.Roads.Strip {
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
    public class RoadStrip: Obj, IObjMesh<RoadStrip>, IRoadElement {
        //ROAD ELEMENT
        public Lane? GetLane() => null;
        public LaneStrip? GetLaneStrip() => null;
        public RoadNode? GetRoadNode() => null;
        public RoadStrip? GetRoadStrip() => this;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => 0;
        public LaneEnd? GetLaneEnd() => null;
        public RoadNodeEnd? GetNodeEnd() => null;

        public readonly Property<StripSplineGenerator> SplineGeneratorProp;
        public StripSplineGenerator SplineGenerator { get => SplineGeneratorProp.Value; set => SplineGeneratorProp.Value = value; }


        // Properties to hold the start and end nodes and their respective lane indices
        public readonly RoadNodeEnd StartNode; // The starting road node of the connection
        public readonly RoadNodeEnd EndNode;

        public readonly Property<RoadFinish> FinishProperty;
        public RoadFinish Finish { get => FinishProperty.Value; set => FinishProperty.Value = value; }

        public RoadStrip(RoadNodeEnd startNode, RoadNodeEnd endNode) {
            StartNode = startNode;
            EndNode = endNode;
            FinishProperty = new(RoadFinish.Embankment, "finish", this);
            SplineGeneratorProp = new(AnisotropicStripSplineGenerator.Instance, "splineformat", this);
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
            return first == StartNode && second == EndNode || first == EndNode && second == StartNode;
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

        public LaneRange FullSizeTag() {
            var bounds = Bounds;
            return new LaneRange(this, new(Bounds.leftStart, Bounds.rightStart), new(Bounds.leftEnd, Bounds.rightEnd));
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

                var startBounds = startLane.lane.Bounds;
                var endBounds = endLane.lane.Bounds;

                bounds = bounds
                    .Update(startBounds.Min, endBounds.Min)
                    .Update(startBounds.Max, endBounds.Max);
            }
            if(bounds.leftStart > bounds.rightStart || bounds.leftEnd > bounds.rightEnd) {
                bounds.leftStart = bounds.rightStart = bounds.leftEnd = bounds.rightEnd = 0;
            }
            segment.Bounds = bounds;

            //Generate splines
            if (segment.IsSingleEnded()) {
                //The segment has only one end
                segment.IndexStrip = segment.StartNode.GenerateDegenerateIndexStrips();
            } else {
                //The segment joins node-ends
                segment.IndexStrip = segment.SplineGenerator.GenerateSplines(segment);
            }
            segment.SplineFrame = segment.IndexStrip.ToSplineFrame(segment.StartNode, segment.EndNode);

            //Check: If the road segment is a part of a road section, do not create its mesh
            var roadSectionA = segment.StartNode.ConnectedSection.Value;
            var roadSectionB = segment.EndNode.ConnectedSection.Value;
            if (roadSectionA != null && roadSectionB != null && roadSectionA == roadSectionB)
                //Belongs to a road section, abort
                return;

            SegmentRenderer.GenerateRoadSegmentFullMesh(segment, mesh); // Otherwise, render the road segment
        }

        public Bezier3 GenerateSpline(float startT, float endT, float y = 0) => GenerateSpline(new Vector3(startT, y, 0), new Vector3(endT, y, 0));
        public Bezier3 GenerateSpline(Vector3 start, Vector3 end) => SplineFrame.CreateFromStartEnd(start, end);


        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public SplineFrame SplineFrame { get; private set; }
        public IndexStrip IndexStrip { get; private set; }
    }
}
