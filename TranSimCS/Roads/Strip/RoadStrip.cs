using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using NLog;
using TranSimCS.Geometry.SplineFrames;
using TranSimCS.Model;
using TranSimCS.Property;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Section;
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

    public struct RoadStripHalf : IEquatable<RoadStripHalf> {
        public RoadStrip RoadStrip;
        public SegmentHalf SegmentHalf;

        public RoadStripHalf(RoadStrip roadStrip, SegmentHalf segmentHalf) {
            RoadStrip = roadStrip;
            SegmentHalf = segmentHalf;
        }
        public RoadStripHalf OppositeHalf() => new(RoadStrip, SegmentHalf.Inverse());

        public override bool Equals(object? obj) {
            return obj is RoadStripHalf half && Equals(half);
        }

        public bool Equals(RoadStripHalf other) {
            return EqualityComparer<RoadStrip>.Default.Equals(RoadStrip, other.RoadStrip) &&
                   SegmentHalf == other.SegmentHalf;
        }

        public override int GetHashCode() {
            return HashCode.Combine(RoadStrip, SegmentHalf);
        }

        public static bool operator ==(RoadStripHalf left, RoadStripHalf right) {
            return left.Equals(right);
        }

        public static bool operator !=(RoadStripHalf left, RoadStripHalf right) {
            return !(left == right);
        }
    }

    /// <summary>
    /// Represents a connection between two road nodes, including lane indices and specifications.
    /// </summary>
    /// <remarks>A <see cref="RoadStrip"/> defines the relationship between two road nodes, specifying
    /// the lanes involved at each node and their respective indices. It also includes properties for lane
    /// specifications and rendering-related data, such as meshes for visualization.</remarks>
    public class RoadStrip: Obj, IObjMesh, IRoadElement, IRoadFinish, IDraggableObj {
        //ROAD ELEMENT
        public Lane? GetLane() => null;
        public LaneStrip? GetLaneStrip() => null;
        public RoadNode? GetRoadNode() => null;
        public RoadStrip? GetRoadStrip() => this;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => 0;
        public LaneEnd? GetLaneEnd() => null;
        public RoadNodeEnd? GetNodeEnd() => null;

        //Managed by TSWorld
        public RoadSection? Section { get; internal set; }

        //Events
        public event EventHandler<RoadStripEventArgs>? OnLaneAdded; // Event triggered when lanes are added or removed
        public event EventHandler<RoadStripEventArgs>? OnLaneRemoved; // Event triggered when lanes are removed
        public event MeshInvalidationCallback GeometryChanged;

        //Road strip contents
        public readonly Property<StripSplineGenerator> SplineGeneratorProp;
        public StripSplineGenerator SplineGenerator { get => SplineGeneratorProp.Value; set => SplineGeneratorProp.Value = value; }
        public readonly RoadNodeEnd StartNode;
        public readonly RoadNodeEnd EndNode;
        public readonly Property<RoadFinish> FinishProperty;
        public RoadFinish Finish { get => FinishProperty.Value; set => FinishProperty.Value = value; }
        Property<RoadFinish> IRoadFinish.FinishProperty => FinishProperty;

        public RoadStrip(RoadNodeEnd startNode, RoadNodeEnd endNode) {
            StartNode = startNode;
            EndNode = endNode;
            FinishProperty = new(RoadFinish.Embankment, "finish", this);
            SplineGeneratorProp = new(AnisotropicStripSplineGenerator.Instance, "splineformat", this);
            Mesh = new MeshGenerator<RoadStrip>(this, GenerateMesh);
            Mesh.OnMeshInvalidated += InvalidateMesh0;
            PropertyChanged += (s, e) => Mesh.Invalidate();
            OnLaneAdded += RoadStrip_OnLaneAdded;
            OnLaneRemoved += RoadStrip_OnLaneRemoved;
        }

        private void RoadStrip_OnLaneRemoved(object? sender, RoadStripEventArgs e) {
            InvalidateNodes();
        }

        private void RoadStrip_OnLaneAdded(object? sender, RoadStripEventArgs e) {
            InvalidateNodes();
        }
        public void InvalidateNodes() {
            StartNode?.Node?.Mesh?.Invalidate();
            EndNode?.Node?.Mesh?.Invalidate();
        }

        public RoadNodeEnd GetHalf(SegmentHalf selectedRoadHalf) => selectedRoadHalf.GetConditional(StartNode, EndNode);

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

        public LaneRange FullSizeTag() {
            var bounds = Bounds;
            return new LaneRange(this, new(Bounds.leftStart, Bounds.rightStart), new(Bounds.leftEnd, Bounds.rightEnd));
        }

        //Meshes for the lane connection (can be used for rendering and cached)
        public MeshGenerator<RoadStrip> Mesh { get; init; }
        protected void InvalidateMesh0() {
            GeometryChanged?.Invoke(this);
            foreach (var lane in lanes)
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
        public Bezier3 GenerateSpline(Vector3 start, Vector3 end) {
            if(StartNode == EndNode) {
                //Generate a solution bypassing the SplineFrame
                var refframe = StartNode.CalcReferenceFrame();
                if (StartNode.End == NodeEnd.Backward) refframe.X *= -1;
                var tfStart = refframe.Transform(start);
                var tfEnd = refframe.Transform(end);
                var distance = Vector3.Distance(start, end);
                var tangent = 0.6667f * refframe.Z * distance;
                return new(tfStart, tfStart + tangent, tfEnd + tangent, tfEnd);
            }
            return SplineFrame.CreateFromStartEnd(start, end);
        }

        IPosition[] IDraggableObj.DraggableComponents() => [StartNode, EndNode];

        public void GenerateGeometry(RenderTarget target) {
            target.Draw(Mesh.GetMesh());
        }
        public BoundingBox GetBounds() => Mesh.GetMesh().GetBounds();
        public bool ComputeIntersection(Ray ray, out float distance, out object? tag) => Mesh.GetMesh().ComputeIntersection(ray, out distance, out tag);

        public SplineFrame SplineFrame { get; private set; }
        public IndexStrip IndexStrip { get; private set; }
    }
}
