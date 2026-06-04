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

namespace TranSimCS.Roads.Node {
    public class RoadNode: Obj, IPosition, IObjMesh<RoadNode>, IRoadElement {
        //Node contents
        public Property<ObjPos> PositionProp { get; private set; }
        public Property<RoadNodeTangents> LeftTangent { get; private set; }
        public Property<RoadNodeTangents> RightTangent { get; private set; }
        public string Name { get; set; }

        //Cahced/generated contents
        public MeshGenerator<RoadNode> Mesh { get; init; }
        public Vector3 CenterPosition { get; private set; }
        public Range<float> Bounds => NodeSpec.Range;
        

        private Dictionary<Guid, Lane> lanesSet;
        private HashSet<Lane> lanesDict;
        public readonly ISet<Lane> Lanes;
        public readonly IDictionary<Guid, Lane> LaneXRef;
        public IList<Lane> SortedLanes { get; private set; }

        //Events
        public class LaneEventArgs: EventArgs {
            public readonly Lane lane;
            public LaneEventArgs(Lane lane) {
                this.lane = lane;
            }
        }
        public event EventHandler<LaneEventArgs> LaneAdded;
        public event EventHandler<LaneEventArgs> LaneRemoved;

        //Bidirectional derived contents
        private NodeSpec? nodeSpec;
        public NodeSpec NodeSpec {
            get {
                nodeSpec ??= new NodeSpec(Lanes.Select(x => x.Definition));
                return nodeSpec;
            }
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
                        existingLane.Definition = newLane;
                    }else {
                        //The lane has been added
                        AddLane(newLane);
                    }
                }
                this.nodeSpec = nodeSpec;
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

        // Constructor to initialize the RoadNode with a unique ID, name, position, and world
        public RoadNode(string name, Vector3 position, int azimuth, float inclination = 0, float tilt = 0) :
            this(name, new ObjPos(position, azimuth, inclination, tilt)) { }
        public RoadNode(string name, ObjPos positionData, Guid? id = null) {
            Guid = id ?? Guid.NewGuid();   
            PositionProp = new(ObjPos.Zero, "Position", this);
            Name = name;
            PositionProp.Value = positionData;
            RearEnd = new RoadNodeEnd(NodeEnd.Backward, this);
            FrontEnd = new RoadNodeEnd(NodeEnd.Forward, this);
            Mesh = new MeshGenerator<RoadNode>(this, (node, mesh) => RoadRenderer.GenerateRoadNodeMesh(node, mesh, 0.4f));
            Mesh.OnMeshInvalidated += InvalidateMesh0;
            PositionProp.ValueChanged += PositionProp_ValueChanged;
            LeftTangent = new(default, "tangentLeft", this);
            RightTangent = new(default, "tangentRight", this);

            lanesSet = new();
            lanesDict = new();
            LaneXRef = new ReadOnlyDictionary<Guid, Lane>(lanesSet);
            Lanes = new ReadOnlySet<Lane>(lanesDict);
        }
        private void PositionProp_ValueChanged(object? sender, PropertyChangedEventArgs2<ObjPos> e) {
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

            Mesh.Invalidate();
        }

        public void ClearLanes() => NodeSpec = NodeSpec.Empty;

        // Clears cached data when the base mesh invalidation occurs.
        protected void InvalidateMesh0(){
            nodeSpec = null;

            //Sort the lanes
            SortedLanes = Lanes.OrderBy(x => x.MiddlePosition).ToImmutableList();
            for (int i = 0; i < SortedLanes.Count; i++) SortedLanes[i].Index = i;

            //Calculate new center position
            var refframe = PositionProp.Value.CalcReferenceFrame();
            CenterPosition = refframe.O + refframe.X * (Bounds.Min + Bounds.Max) * 0.5f;

            foreach (var connection in Connections) connection.Mesh.Invalidate();
            RearEnd.ConnectedSection.Value?.Regenerate();
            FrontEnd.ConnectedSection.Value?.Regenerate();
        }

        //Halves of this road node
        public readonly RoadNodeEnd RearEnd;
        public readonly RoadNodeEnd FrontEnd;
        // Retrieves the node end instance for the given direction.
        public RoadNodeEnd GetEnd(NodeEnd end) => end.GetConditional(RearEnd, FrontEnd);

        //Connections (maintained by the node ends)
        public IEnumerable<RoadStrip> Connections => RearEnd.ConnectedSegments.Union(FrontEnd.ConnectedSegments);

        public Vector3 CenterOffset { get; internal set; }

        public const Lane? nullLane = null;
        public Lane? LastLane => (lanesDict.Count == 0) ? null : SortedLanes[^1];
    }
}
