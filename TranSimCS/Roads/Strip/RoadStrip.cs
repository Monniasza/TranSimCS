using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using NLog;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Property;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Section;
using TranSimCS.Roads.StripGenerator;
using TranSimCS.SceneGraph;
using TranSimCS.Setting;
using TranSimCS.Spatial;
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
        public readonly HalfNode StartNode;
        public readonly HalfNode EndNode;
        public readonly Property<RoadFinish> FinishProperty;
        public RoadFinish Finish { get => FinishProperty.Value; set => FinishProperty.Value = value; }
        Property<RoadFinish> IRoadFinish.FinishProperty => FinishProperty;
        private List<LaneStrip> lanes = new(); // List of lane strips associated with this road connection
        public IReadOnlyCollection<LaneStrip> Lanes => lanes.AsReadOnly(); // Get the list of lane strips associated with this road connection

        //Cached properties
        private RoadStripCache? _cache;
        public RoadStripCache Cache => _cache ??= new RoadStripCache(this);
        /// <summary>
        /// Left start, right start, left end, right end
        /// </summary>
        public RoadBounds Bounds => Cache.Bounds;
        public OrthodistantBasis OrthodistantBasis => Cache.OrthodistantBasis;
        public IndexSpline IndexStrip => Cache.IndexStrip;
        public LaneRange FullSizeTag() {
            var bounds = Bounds;
            return new LaneRange(this, new(Bounds.leftStart, Bounds.rightStart), new(Bounds.leftEnd, Bounds.rightEnd));
        }

        public RoadStrip(HalfNode startNode, HalfNode endNode) {
            StartNode = startNode;
            EndNode = endNode;
            FinishProperty = new(RoadFinish.Embankment, "finish", this);
            SplineGeneratorProp = new(AnisotropicStripSplineGenerator.Instance, "splineformat", this);
            Mesh = new MeshGenerator<RoadStrip>(this, GenerateMesh);
            Mesh.OnMeshInvalidated += InvalidateMesh0;
        }

        public HalfNode GetHalf(SegmentHalf selectedRoadHalf) => selectedRoadHalf.GetConditional(StartNode, EndNode);

        public bool CheckEnds(HalfNode first, HalfNode second) {
            return first == StartNode && second == EndNode || first == EndNode && second == StartNode;
        }

        
        public void AddLaneStrip(LaneStrip laneStrip) {
            if(!MaybeAddLaneStrip(laneStrip)) throw new ArgumentException("Lanes must not be duplicated");
        }
        public bool RemoveLaneStrip(LaneStrip laneStrip) {
            var removal = lanes.Remove(laneStrip); // Remove a lane strip from the connection
            if(!removal) return false;
            laneStrip.Road = null;
            OnLaneRemoved?.Invoke(this, new RoadStripEventArgs(laneStrip)); // Trigger the OnLaneRemoved event
            FirePropertyEvent(this, new(PropertyNames.SegmentLanes));
            Mesh.Invalidate(); // Invalidate the mesh for the lane strip to ensure it is regenerated
            return true;
        }
        public bool MaybeAddLaneStrip(LaneStrip laneStrip) {
            if (lanes.Contains(laneStrip)) return false;
            lanes.Add(laneStrip); // Add a new lane strip to the connection
            laneStrip.Road = this;
            OnLaneAdded?.Invoke(this, new RoadStripEventArgs(laneStrip)); // Trigger the OnLaneAdded event
            FirePropertyEvent(this, new(PropertyNames.SegmentLanes));
            Mesh.Invalidate(); // Invalidate the mesh for the lane strip to ensure it is regenerated
            return true;
        }

        

        //Meshes for the lane connection (can be used for rendering and cached)
        public MeshGenerator<RoadStrip> Mesh { get; init; }
        protected void InvalidateMesh0() {
            _cache = null;
            foreach (var lane in lanes)
                lane.InvalidateMesh(); // Invalidate the mesh for each lane strip
            GeometryChanged?.Invoke(this);
        }
        protected static void GenerateMesh(RoadStrip segment, MultiMesh mesh) {

            //Check: If the road segment is a part of a road section, do not create its mesh
            var roadSectionA = segment.StartNode.ConnectedSection.Value;
            var roadSectionB = segment.EndNode.ConnectedSection.Value;
            if (roadSectionA != null && roadSectionB != null && roadSectionA == roadSectionB)
                //Belongs to a road section, abort
                return;

            SegmentRenderer.GenerateRoadSegmentFullMesh(segment, mesh); // Otherwise, render the road segment
        }

        public Vector3[] GenerateSpline(float startT, float endT, float y = 0) => GenerateSpline(new Vector3(startT, y, 0), new Vector3(endT, y, 0));
        public OrthodistantBasis GenerateOrthodistant(float startT, float endT) => new(OrthodistantBasis.ReferenceSpline, OrthodistantBasis.NormalSpline, OrthodistantBasis.StartEndPosition + new Vector2(startT, endT));
        public Vector3[] GenerateSpline(Vector3 start, Vector3 end) {
            var accuracy = Settings.RoadAccuracy;
            float step = 1 / (accuracy - 1.0f);
            var result = new Vector3[accuracy];
            if (StartNode == EndNode) {
                //Generate a solution bypassing the OrthonormalBasis
                var refframe = StartNode.Cache.ReferenceFrame;
                var centerOfRevolutionT = (start + end) / 2;
                var xBasis = (end - start) / 2;
                var yBasis = refframe.Y.Orthogonalize(xBasis).Normalized() * xBasis.Length();
                float radianStep = MathF.PI * step;
                for(int i = 0; i < accuracy; i++) {
                    var (sin, cos) = MathF.SinCos(i * radianStep);
                    var point = centerOfRevolutionT + xBasis * cos + yBasis * sin;
                    result[i] = point;
                }
            } else {
                //Sample the OrthonormalBasis
                for(int i = 0; i < accuracy; i++) {
                    var t = i * step;
                    var sample = OrthodistantBasis.Sample(t, start, end);
                    result[i] = sample.O;
                }
            }
            return result;
        }

        IPosition[] IDraggableObj.DraggableComponents() => [StartNode, EndNode];

        public void GenerateGeometry(RenderTarget target) {
            target.Draw(Mesh.GetMesh());
        }
        public BoundingBox GetBounds() => Mesh.GetMesh().GetBounds();
        public bool ComputeIntersection(Ray ray, out float distance, out object? tag) {
            if (Section != null) return IBVHElement.Reject(ray, out distance, out tag);
            return Mesh.GetMesh().ComputeIntersection(ray, out distance, out tag);
        }

        
    }
}
