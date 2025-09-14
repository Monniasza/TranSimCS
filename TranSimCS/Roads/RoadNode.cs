using System;
using System.Collections.Generic;
using System.Linq;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Model;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public class NodePositionChangedEventArgs(ObjPos oldPosition, ObjPos newPosition) : EventArgs {
        public ObjPos OldPosition { get; } = oldPosition;
        public ObjPos NewPosition { get; } = newPosition;
    }

    public enum NodeEnd {
        Forward, Backward
    }

    public class RoadNodeEnd: IPosition {
        //Constructor
        public readonly NodeEnd End;
        public readonly RoadNode Node;
        internal RoadNodeEnd(NodeEnd end, RoadNode node) {
            End = end;
            Node = node;
            ConnectedSection.ValueChanged += ConnectedSection_ValueChanged;
        }

        private void ConnectedSection_ValueChanged(object sender, PropertyChangedEventArgs2<RoadSection> e) {
            var oldNode = e.OldValue;
            oldNode?.OnDisconnect(this);
            var newNode = e.NewValue;
            newNode?.OnConnect(this);
        }

        public RoadNodeEnd OppositeEnd => Node.GetEnd(End.Negate());

        //Indexing component for the road node, maintained by the World class
        internal ISet<RoadStrip> connectionsOld = new HashSet<RoadStrip>(); // Connections to other road segments
        public ISet<RoadStrip> ConnectionsOld => new ReadOnlySet<RoadStrip>(connectionsOld); // Expose the connections set

        //Indexing of the road sections
        public Property<RoadSection> ConnectedSection { get; } = new Property<RoadSection>(null, "connection");

        //Position
        public Property<ObjPos> PositionProp => Node.PositionProp;
        public Vector3 CenterPosition => Node.CenterPosition;
        public Vector3 CenterOffset => Node.CenterOffset;

        public LaneEnd GetLaneEnd(int x) {
            return new LaneEnd(End, Node.Lanes[x]);
        }

        public Vector2 Bounds() {
            float lbound = Node.Lanes[0].LeftPosition;
            float rbound = Node.Lanes[Node.Lanes.Count-1].RightPosition;
            if (End == NodeEnd.Backward) (lbound, rbound) = (rbound, lbound);
            return new Vector2(lbound, rbound);
        }
    }

    public class RoadNode: Obj, IPosition {

        public Property<ObjPos> PositionProp { get; private set; }

        //Example azimuth values
        public const int AZIMUTH_NORTH = 0; // 0 degrees
        public const int AZIMUTH_EAST = 1 << 30; // 90 degrees
        public const int AZIMUTH_SOUTH = 2 << 30; // 180 degrees
        public const int AZIMUTH_WEST = 3 << 30; // 270 degrees

        //Identifiers
        public string Name { get; set; }
        public World World { get; init; }

        //Lane structure
        private readonly List<Lane> _lanes = new List<Lane>(); // List to hold lanes associated with this road node
        public IReadOnlyList<Lane> Lanes => _lanes.AsReadOnly(); // Expose the lanes as a read-only list
        public void AddLane(Lane lane) {
            if(lane == null) throw new ArgumentNullException(nameof(lane), "Lane cannot be null.");
            if(lane.RoadNode != this) throw new ArgumentException("Lane does not belong to this road node.", nameof(lane));
            var middlePosition = (lane.LeftPosition + lane.RightPosition) / 2; // Calculate the middle position of the lane
            int index = _lanes.FindIndex(lane1 => lane1.MiddlePosition > middlePosition); // Find the index where the lane should be inserted
            if (index == -1) index = Lanes.Count;
            //Shift existing lanes to the right if necessary
            var lanesToShift = _lanes.Skip(index).ToList(); // Get the lanes that will be shifted
            foreach (var l in lanesToShift) 
                l.Index++; // Increment the index of each lane that will be shifted
            lane.Index = index; // Set the index of the new lane
            _lanes.Insert(index, lane);// Add the lane to the list
            InvalidateMesh();
        }
        public void RemoveLane(Lane lane) {
            if(lane == null) throw new ArgumentNullException(nameof(lane), "Lane cannot be null.");
            if(!_lanes.Remove(lane)) throw new ArgumentException("Lane does not belong to this road node.", nameof(lane));
            //Shift existing lanes to the left if necessary
            var lanesToShift = _lanes.Skip(lane.Index).ToList(); // Get the lanes that will be shifted
            foreach (var l in lanesToShift) 
                l.Index--; // Decrement the index of each lane that will be shifted

            //Remove connected lanes from the connections
            var connections = lane.Connections.ToArray();
            foreach(var connection in connections) {
                connection.Destroy();
            }
            lane.connections.Clear(); // Clear the connections of the lane being removed
            if(Lanes.Count == 0) {
                //Demolish the node
                World.RoadNodes.Remove(this);
            }
            InvalidateMesh();
        }
        public void ClearLanes() {
            var lanes = _lanes.ToArray();
            foreach(var lane in lanes) RemoveLane(lane);
        }

        //Node selection mesh
        protected override void GenerateMesh(Mesh mesh) {
            RoadRenderer.GenerateRoadNodeMesh(this, mesh, 0.001f);
        }
        private void InvalidateMeshes() {
            InvalidateMesh();
            foreach (var connection in Connections) connection.InvalidateMesh();
        }
        protected override void InvalidateMesh0(){
            _centerPos = null;
        }

        //Halves of this road node
        public readonly RoadNodeEnd RearEnd;
        public readonly RoadNodeEnd FrontEnd;
        public RoadNodeEnd GetEnd(NodeEnd end) => end.GetConditional(RearEnd, FrontEnd);

        //Connections (maintained by the node ends)
        public IEnumerable<RoadStrip> Connections => RearEnd.ConnectionsOld.Union(FrontEnd.ConnectionsOld);

        //Center position
        private void CalcCenterPos(){
            if (Lanes.Count == 0) {
                _centerPos = PositionProp.Value.Position;
            } else {
                var leftPos = Lanes[0].LeftPosition;
                var rightPos = Lanes[Lanes.Count - 1].RightPosition;
                _centerPos = Geometry.calcLineEnd(FrontEnd, (leftPos + rightPos) / 2).Position;
            }
        }
        public Vector3? _centerPos;
        public Vector3 CenterPosition { get {
            if (_centerPos == null) CalcCenterPos();
            return _centerPos.Value;
        } }
        public Vector3 CenterOffset { get; internal set; }

        public Lane LastLane => Lanes[Lanes.Count - 1];

        

        // Constructor to initialize the RoadNode with a unique ID, name, position, and world
        public RoadNode(World world, string name, Vector3 position, int azimuth, float inclination = 0, float tilt = 0) :
            this(world, name, new ObjPos(position, azimuth, inclination, tilt)) { }
        public RoadNode(World world, string name, ObjPos positionData) {
            PositionProp = new(ObjPos.Zero, "Position", this);
            Name = name;
            PositionProp.Value = positionData;
            World = world;
            RearEnd = new RoadNodeEnd(NodeEnd.Backward, this);
            FrontEnd = new RoadNodeEnd(NodeEnd.Forward, this);
            PositionProp.ValueChanged += (sender, e) => InvalidateMeshes();
        }
    }
}
