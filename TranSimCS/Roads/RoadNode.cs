using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.SceneGraph;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Roads {
    public class NodePositionChangedEventArgs(ObjPos oldPosition, ObjPos newPosition) : EventArgs {
        public ObjPos OldPosition { get; } = oldPosition;
        public ObjPos NewPosition { get; } = newPosition;
    }

    public class RoadNode: Obj, IPosition, IObjMesh<RoadNode> {
        public Property<ObjPos> PositionProp { get; private set; }

        //Example azimuth values
        public const int AZIMUTH_NORTH = 0; // 0 degrees
        public const int AZIMUTH_EAST = 1 << 30; // 90 degrees
        public const int AZIMUTH_SOUTH = 2 << 30; // 180 degrees
        public const int AZIMUTH_WEST = 3 << 30; // 270 degrees

        //Identifiers
        public string Name { get; set; }
        public TSWorld World { get; internal set; }

        // Constructor to initialize the RoadNode with a unique ID, name, position, and world
        public RoadNode(TSWorld world, string name, Vector3 position, int azimuth, float inclination = 0, float tilt = 0) :
            this(world, name, new ObjPos(position, azimuth, inclination, tilt)) { }
        public RoadNode(TSWorld world, string name, ObjPos positionData, Guid? id = null) {
            Guid = id ?? Guid.NewGuid();
            PositionProp = new(ObjPos.Zero, "Position", this);
            PositionProp.ValueChanged += PositionProp_ValueChanged;
            Name = name;
            PositionProp.Value = positionData;
            World = world;
            RearEnd = new RoadNodeEnd(NodeEnd.Backward, this);
            FrontEnd = new RoadNodeEnd(NodeEnd.Forward, this);
            PositionProp.ValueChanged += (sender, e) => InvalidateMeshes();
            Mesh = new MeshGenerator<RoadNode>(this, GenerateMesh);
        }

        private void PositionProp_ValueChanged(object? sender, PropertyChangedEventArgs2<ObjPos> e) {
            var value = e.NewValue;
            var pos = value.Position;
            if (float.IsNaN(pos.X)) throw new ArgumentException("X === NaN");
            if (float.IsNaN(pos.Y)) throw new ArgumentException("Y === NaN");
            if (float.IsNaN(pos.Z)) throw new ArgumentException("Z === NaN");
        }

        //Lane structure
        private readonly List<Lane> _lanes = new List<Lane>(); // List to hold lanes associated with this road node
        public IReadOnlyList<Lane> Lanes => _lanes.AsReadOnly(); // Expose the lanes as a read-only list
        // Adds a lane to this node while maintaining lane ordering and indices.
        public void AddLane(Lane lane) {
            if(lane == null) throw new ArgumentNullException(nameof(lane), "Lane cannot be null.");
            var currentNode = lane.RoadNode;
            if (currentNode != null) {
                if (currentNode == this) {
                    if (_lanes.Contains(lane)) throw new InvalidOperationException("Lane is already assigned to this road node.");
                } else {
                    currentNode.RemoveLane(lane);
                }
            }
            lane.RoadNode = this;
            var middlePosition = (lane.LeftPosition + lane.RightPosition) / 2; // Calculate the middle position of the lane
            int index = _lanes.FindIndex(lane1 => lane1.MiddlePosition > middlePosition); // Find the index where the lane should be inserted
            if (index == -1) index = Lanes.Count;
            //Shift existing lanes to the right if necessary
            _lanes.Insert(index, lane);// Add the lane to the list
            ReIndex();
            Mesh.Invalidate();
        }
        // Removes a lane from this node and clears related connections.
        public void RemoveLane(Lane lane) {
            if(lane == null) throw new ArgumentNullException(nameof(lane), "Lane cannot be null.");
            var index = _lanes.IndexOf(lane);
            if (index < 0) throw new InvalidOperationException("Lane is not assigned to this road node.");

            var connections = lane.Connections.ToArray();
            lane.RoadNode = null;

            foreach(var connection in connections) {
                connection.Destroy();
            }
            lane.connections.Clear();

            _lanes.RemoveAt(index);
            lane.Index = -1;
            ReIndex();

            if(Lanes.Count == 0) {
                World.Nodes.data.Remove(this);
            }
            Mesh.Invalidate();
        }

        private void ReIndex() {
            for (int i = 0; i < _lanes.Count; i++) {
                _lanes[i].Index = i;
            }
        }

        // Clears all lanes by delegating to RemoveLane for each entry.
        public void ClearLanes() {
            var lanes = _lanes.ToArray();
            foreach(var lane in lanes) RemoveLane(lane);
        }

        //Node selection mesh
        public MeshGenerator<RoadNode> Mesh {  get; init; }
        // Generates the mesh used for node selection rendering.
        protected static void GenerateMesh(RoadNode node, MultiMesh mesh) {
            // Use 0.4f offset to render nodes clearly above all other road elements (roads at 0.2f, intersections at 0.3f)
            RoadRenderer.GenerateRoadNodeMesh(node, mesh, 0.4f);
        }
        // Invalidates this node and its connected strips to trigger mesh rebuilding.
        private void InvalidateMeshes() {
            Mesh.Invalidate();
            foreach (var connection in Connections) connection.Mesh.Invalidate();
        }
        // Clears cached data when the base mesh invalidation occurs.
        protected static void InvalidateMesh0(RoadNode node){
            node._centerPos = null;
        }

        //Halves of this road node
        public readonly RoadNodeEnd RearEnd;
        public readonly RoadNodeEnd FrontEnd;
        // Retrieves the node end instance for the given direction.
        public RoadNodeEnd GetEnd(NodeEnd end) => end.GetConditional(RearEnd, FrontEnd);

        //Connections (maintained by the node ends)
        public IEnumerable<RoadStrip> Connections => RearEnd.ConnectedSegments.Union(FrontEnd.ConnectedSegments);

        //Center position
        // Calculates and caches the node center based on lane positions.
        private void CalcCenterPos(){
            if (Lanes.Count == 0) {
                _centerPos = PositionProp.Value.Position;
            } else {
                var leftPos = Lanes[0].LeftPosition;
                var rightPos = Lanes[Lanes.Count - 1].RightPosition;
                _centerPos = LineEnd.calcLineEnd(FrontEnd, (leftPos + rightPos) / 2).Position;
            }
        }

        public Vector3? _centerPos;
        // Returns the cached center position, computing it when necessary.
        public Vector3 CenterPosition { get {
            if (_centerPos == null) CalcCenterPos();
            return _centerPos.Value;
        } }
        public Vector3 CenterOffset { get; internal set; }

        public Lane? LastLane => _lanes.Count > 0 ? _lanes[^1] : null;



        
    }
}
