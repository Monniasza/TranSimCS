using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Property;
using TranSimCS.Roads;
using TranSimCS.Roads.Strip;
using TranSimCS.SceneGraph;
using TranSimCS.Worlds;
using Transform3 = TranSimCS.Geometry.Transform3;

namespace TranSimCS.Roads.Node {
    public class RoadNode: Obj, IPosition, IObjMesh, IRoadElement {
        //Node contents
        public Property<PositionEulerAngles> PositionProp { get; private set; }
        public string Name { get; set; }

        //Cahced/generated contents
        public MeshGenerator<RoadNode> Mesh { get; init; }
        public MeshGenerator<RoadNode> SelectionMesh { get; init; }

        private RoadNodeCache? _cache;
        public RoadNodeCache Cache => _cache ??= new RoadNodeCache(this);
        public Vector3 CenterPosition => Cache.CenterPosition;
        public Range<float> Bounds => NodeSpec.Range;
        //NodeSpec
        public Transform3 ReferenceFrame => Cache.ReferenceFrame;
        public IList<Lane> SortedLanes => Cache.SortedLanes;

        //Lane contents maintained by the RoadNode
        private Dictionary<Guid, Lane> lanesSet;
        private HashSet<Lane> lanesDict;
        public readonly ISet<Lane> Lanes;
        public readonly IDictionary<Guid, Lane> LaneXRef;

        //Events
        public class LaneEventArgs: EventArgs {
            public readonly Lane lane;
            public LaneEventArgs(Lane lane) {
                this.lane = lane;
            }
        }
        public event EventHandler<LaneEventArgs> LaneAdded;
        public event EventHandler<LaneEventArgs> LaneRemoved;
        public event MeshInvalidationCallback GeometryChanged;

        //Bidirectional derived contents
        public NodeSpec NodeSpec {
            get => Cache.NodeSpec;
            set {
                //Cross-relate the lanes to change
                var nodeSpec = value;
                var lanes2check = Lanes.ToArray();
                foreach (var lane in lanes2check) {
                    //Check which lanes have been removed
                    if (!nodeSpec.LaneXRef.ContainsKey(lane.Guid))
                        //The lane has been removed
                        RemoveLane(lane);
                }
                foreach(var newLane in nodeSpec) {
                    if(LaneXRef.TryGetValue(newLane.ID, out var existingLane)) {
                        //The lane has been changed
                        existingLane.LaneNode = newLane;
                    }else {
                        //The lane has been added
                        AddLane(newLane);
                    }
                }
            }
        }

        //ROAD ELEMENT
        public Lane? GetLane() => null;
        public LaneStrip? GetLaneStrip() => null;
        public RoadNode? GetRoadNode() => this;
        public RoadStrip? GetRoadStrip() => null;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => 0;
        public LaneEnd? GetLaneEnd() => null;
        public RoadNodeEnd? GetNodeEnd() => null;

        //Example azimuth values
        public const int AZIMUTH_NORTH = 0; // 0 degrees
        public const int AZIMUTH_EAST = 1 << 30; // 90 degrees
        public const int AZIMUTH_SOUTH = 2 << 30; // 180 degrees
        public const int AZIMUTH_WEST = 3 << 30; // 270 degrees

        public RoadNode(string name, PositionEulerAngles positionData, Guid? id = null) {
            Guid = id ?? Guid.NewGuid();   
            PositionProp = new(PositionEulerAngles.Zero, "Position", this);
            Name = name;
            PositionProp.Value = positionData;
            RearEnd = new RoadNodeEnd(NodeEnd.Backward, this);
            FrontEnd = new RoadNodeEnd(NodeEnd.Forward, this);
            RearHalf = new HalfNode(this, NodeEnd.Backward);
            FrontHalf = new HalfNode(this, NodeEnd.Forward);
            Mesh = new MeshGenerator<RoadNode>(this, (node, mesh) => NodeRenderer.GenerateNodeVisualMesh(node, mesh));
            Mesh.OnMeshInvalidated += InvalidateMesh0;
            SelectionMesh = new(this, (node, mesh) => {
                var roadBin = mesh.GetOrCreateRenderBinForced(Assets.Road);
                NodeRenderer.GenerateRoadNodeSelectionMesh(node, roadBin, null);
            });
            PositionProp.ValueChanged += PositionProp_ValueChanged;

            lanesSet = new();
            lanesDict = new();
            LaneXRef = new ReadOnlyDictionary<Guid, Lane>(lanesSet);
            Lanes = new ReadOnlySet<Lane>(lanesDict);
        }
        private void PositionProp_ValueChanged(object? sender, PropertyChangedEventArgs2<PositionEulerAngles> e) {
            var value = e.NewValue;
            var pos = value.Position;
            if (float.IsNaN(pos.X)) throw new ArgumentException("X === NaN");
            if (float.IsNaN(pos.Y)) throw new ArgumentException("Y === NaN");
            if (float.IsNaN(pos.Z)) throw new ArgumentException("Z === NaN");
        }

        //Lane structure
        // Adds a lane to this node while maintaining lane ordering and indices.
        public Lane AddLane(LaneNode definition) {
            if(definition == null) throw new ArgumentNullException(nameof(definition), "Lane cannot be null.");
            if(LaneXRef.ContainsKey(definition.ID)) throw new InvalidOperationException("Lane is already assigned to this road node.");

            Lane lane = new Lane(this, definition);
            lane.RoadNode = this;

            lanesSet.Add(definition.ID, lane);
            lanesDict.Add(lane);

            LaneAdded?.Invoke(this, new LaneEventArgs(lane));
            FrontHalf.FireLaneAdded(lane);
            RearHalf.FireLaneAdded(lane);

            Mesh.Invalidate();

            return lane;
        }
        // Removes a lane from this node and clears related connections.
        public void RemoveLane(Lane lane) {
            if(lane == null) throw new ArgumentNullException(nameof(lane), "Lane cannot be null.");
            if(!Lanes.Contains(lane)) throw new InvalidOperationException("Lane is not assigned to this road node.");

            //Remove all connections to the lane
            var connections = lane.Connections.ToArray();
            foreach (var connection in connections) {
                connection.Destroy();
            }

            lanesSet.Remove(lane.Guid);
            lanesDict.Remove(lane);
            lane.Index = -1;
            lane.RoadNode = null;

            LaneRemoved?.Invoke(this, new LaneEventArgs(lane));
            FrontHalf.FireLaneRemoved(lane);
            RearHalf.FireLaneRemoved(lane);

            Mesh.Invalidate();
        }

        public void ClearLanes() => NodeSpec = NodeSpec.Empty;

        // Clears cached data when the base mesh invalidation occurs.
        protected void InvalidateMesh0(){
            _cache = null;
            SelectionMesh.Invalidate();
            GeometryChanged?.Invoke(this);

            foreach (var connection in Connections) connection.Mesh.Invalidate();
            RearEnd.ConnectedSection.Value?.Regenerate();
            FrontEnd.ConnectedSection.Value?.Regenerate();
        }

        //Halves of this road node
        public readonly RoadNodeEnd RearEnd;
        public readonly RoadNodeEnd FrontEnd;

        public HalfNode FrontHalf { get; private set; }
        public HalfNode RearHalf { get; private set; }
        public HalfNode GetHalfNode(NodeEnd end) => end.GetConditional(RearHalf, FrontHalf);


        // Retrieves the node end instance for the given direction.
        public RoadNodeEnd GetEnd(NodeEnd end) => end.GetConditional(RearEnd, FrontEnd);

        public void GenerateGeometry(RenderTarget target) => target.Draw(Mesh.GetMesh());

        public BoundingBox GetBounds() => SelectionMesh.GetMesh().GetBounds();

        public bool ComputeIntersection(Ray ray, out float distance, out object? tag) => SelectionMesh.GetMesh().ComputeIntersection(ray, out distance, out tag);

        //Connections (maintained by the node ends)
        public IEnumerable<RoadStrip> Connections => RearEnd.ConnectedSegments.Union(FrontEnd.ConnectedSegments);

        public Vector3 CenterOffset { get; internal set; }

        public const Lane? nullLane = null;
        public Lane? LastLane => (lanesDict.Count == 0) ? null : SortedLanes[^1];
    }
}
