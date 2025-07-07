using System;
using System.Collections.Generic;
using System.Linq;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public class NodePositionChangedEventArgs(ObjPos oldPosition, ObjPos newPosition) : EventArgs {
        public ObjPos OldPosition { get; } = oldPosition;
        public ObjPos NewPosition { get; } = newPosition;
    }

    public enum NodeEnd {
        Forward, Backward
    }

    public class RoadNodeEnd {
        //Constructor
        public readonly NodeEnd End;
        public readonly RoadNode Node;
        internal RoadNodeEnd(NodeEnd end, RoadNode node) {
            End = end;
            Node = node;
        }

        public RoadNodeEnd OppositeEnd => Node.GetEnd(End.Negate());

        //Indexing component for the road node, maintained by the World class
        internal ISet<RoadStrip> connections = new HashSet<RoadStrip>(); // Connections to other road segments
        public ISet<RoadStrip> Connections => new ReadOnlySet<RoadStrip>(connections); // Expose the connections set

        public LaneEnd GetLaneEnd(int x) {
            return new LaneEnd(End, Node.Lanes[x]);
        }
    }

    public class RoadNode {

        //Example azimuth values
        public const int AZIMUTH_NORTH = 0; // 0 degrees
        public const int AZIMUTH_EAST = 1 << 30; // 90 degrees
        public const int AZIMUTH_SOUTH = 2 << 30; // 180 degrees
        public const int AZIMUTH_WEST = 3 << 30; // 270 degrees

        //Identifiers
        private static int _nextId = 1; // Static field to keep track of the next ID
        public int Id { get; init; }
        public string Name { get; set; }
        public World World { get; init; }

        //World position of the road node
        public event EventHandler<NodePositionChangedEventArgs> PositionChanged; // Event to notify when the position changes
        private ObjPos _position; // Backing field for the position
        public ObjPos PositionData {
            get => _position;
            set {
                if (_position != value) {
                    var oldPosition = _position;
                    _position = value;
                    PositionChanged?.Invoke(this, new NodePositionChangedEventArgs(oldPosition, value)); // Raise the event with old and new position
                    InvalidateMesh();
                }
            }
        }

        //Lane structure
        private readonly List<Lane> _lanes = new List<Lane>(); // List to hold lanes associated with this road node
        public IReadOnlyList<Lane> Lanes => _lanes.AsReadOnly(); // Expose the lanes as a read-only list
        public void AddLane(Lane lane) {
            if(lane == null) throw new ArgumentNullException(nameof(lane), "Lane cannot be null.");
            if(lane.RoadNode != this) throw new ArgumentException("Lane does not belong to this road node.", nameof(lane));
            var middlePosition = (lane.LeftPosition + lane.RightPosition) / 2; // Calculate the middle position of the lane
            int index = _lanes.FindIndex(lane1 => lane1.MiddlePosition > middlePosition); // Find the index where the lane should be inserted
            //Shift existing lanes to the right if necessary
            var lanesToShift = _lanes.Skip(index).ToList(); // Get the lanes that will be shifted
            foreach (var l in lanesToShift) 
                l.Index++; // Increment the index of each lane that will be shifted
            lane.Index = index; // Set the index of the new lane
            _lanes.Add(lane); // Add the lane to the list
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
            InvalidateMesh();
        }
        public void ClearLanes() {
            var lanes = _lanes.ToArray();
            foreach(var lane in lanes) RemoveLane(lane);
        }

        //Node selection mesh
        private Mesh mesh;
        public Mesh GetMesh() {
            if(mesh == null) {
                mesh = new Mesh();
                RoadRenderer.GenerateRoadNodeMesh(this, mesh, 0.001f);
            }
            return mesh;
        }
        public void InvalidateMesh() {
            mesh = null;
        }

        //Halves of this road node
        public readonly RoadNodeEnd RearEnd;
        public readonly RoadNodeEnd FrontEnd;
        public RoadNodeEnd GetEnd(NodeEnd end) => end.GetConditional(RearEnd, FrontEnd);

        //Connections (maintained by the node ends)
        public IEnumerable<RoadStrip> Connections => RearEnd.Connections.Union(FrontEnd.Connections);

        // Constructor to initialize the RoadNode with a unique ID, name, position, and world
        public RoadNode(World world, string name, Vector3 position, int azimuth, float inclination = 0, float tilt = 0) :
            this(world, name, new ObjPos(position, azimuth, inclination, tilt)) { }
        public RoadNode(World world, string name, ObjPos positionData) {
            Id = _nextId++;
            Name = name;
            PositionData = positionData; // Set the position data
            World = world;
            RearEnd = new RoadNodeEnd(NodeEnd.Backward, this);
            FrontEnd = new RoadNodeEnd(NodeEnd.Forward, this);
        }
    }
}
