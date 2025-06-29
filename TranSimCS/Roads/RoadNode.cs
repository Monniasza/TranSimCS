using System;
using System.Collections.Generic;
using System.Linq;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public struct NodePosition {
        //Position
        public Vector3 Position { get; set; } // World position of the node

        //Angle
        public int Azimuth { get; set; } // Azimuth angle in the 2^32 field
        public float Inclination { get; set; } // Inclination angle in radians
        public float Tilt { get; set; } // Tilt angle in radians

        //Curvatures
        public float HCurvature { get; set; } // Curvature of the road node in rad/meter clockwise
        public float VCurvature { get; set; } // Vertical curvature of the road node in rad/meter upwards
        public float TiltCurvature { get; set; } // Tilt curvature of the road node in rad/meter

        public Vector3 Curvature {
            readonly get => new(HCurvature, VCurvature, TiltCurvature); set { 
            HCurvature = value.X;
            VCurvature = value.Y;
            TiltCurvature = value.Z;
        } }

        public static NodePosition Zero => new NodePosition(Vector3.Zero, 0);

        public NodePosition(Vector3 position, int azimuth, float inclination = 0f, float tilt = 0f, float hCurvature = 0, float vCurvature = 0, float tiltCurvature = 0) {
            Position = position;
            Azimuth = azimuth;
            Inclination = inclination;
            Tilt = tilt;
            HCurvature = hCurvature; // Horizontal curvature in rad/meter clockwise
            VCurvature = vCurvature;
            TiltCurvature = tiltCurvature; // Tilt curvature in rad/meter
        }

        public override bool Equals(object obj) {
            if (obj is NodePosition other) {
                return Position.Equals(other.Position) &&
                       Azimuth == other.Azimuth &&
                       Inclination.Equals(other.Inclination) &&
                       Tilt.Equals(other.Tilt) &&
                       HCurvature.Equals(other.HCurvature) &&
                       VCurvature.Equals(other.VCurvature) &&
                       TiltCurvature.Equals(other.TiltCurvature);
            }
            return false;
        }
        public override int GetHashCode() {
            HashCode hash = new HashCode();
            hash.Add(Position);
            hash.Add(Azimuth);
            hash.Add(Inclination);
            hash.Add(Tilt);
            hash.Add(HCurvature);
            hash.Add(VCurvature);
            hash.Add(TiltCurvature);
            return hash.ToHashCode(); // Generate a hash code based on the properties of the node position
        }
        public static bool operator ==(NodePosition left, NodePosition right) {
            return left.Equals(right);
        }
        public static bool operator !=(NodePosition left, NodePosition right) {
            return !(left == right);
        }

        public Transform3 CalcReferenceFrame() {
            Matrix matrix = Matrix.CreateFromYawPitchRoll(Geometry.FieldToRadians(Azimuth), Inclination, Tilt) * Matrix.CreateTranslation(Position);
            return new Transform3(matrix);
        }

        public static NodePosition FromPosTangentTilt(Vector3 pos, Vector3 tangent, float tilt) {
            var htangent = Geometry.hypot2(tangent.X, tangent.Z);
            var inclination = MathF.Atan2(tangent.Y, htangent);
            var azimuthRadians = MathF.Atan2(tangent.X, tangent.Z);
            var azimuth = Geometry.RadiansToField(azimuthRadians);
            return new NodePosition(pos, azimuth, inclination, tilt);
        }
    }

    public class NodePositionChangedEventArgs(NodePosition oldPosition, NodePosition newPosition) : EventArgs {
        public NodePosition OldPosition { get; } = oldPosition;
        public NodePosition NewPosition { get; } = newPosition;
    }

    public enum NodeEnd {
        Forward, Backward
    }

    public struct RoadNodeEnd(RoadNode node, NodeEnd end) : IEquatable<RoadNodeEnd> {
        public NodeEnd End { get; } = end;
        public RoadNode Node { get; } = node;

        public override bool Equals(object obj) {
            return obj is RoadNodeEnd end && Equals(end);
        }

        public bool Equals(RoadNodeEnd other) {
            return End == other.End &&
                   EqualityComparer<RoadNode>.Default.Equals(Node, other.Node);
        }

        public override int GetHashCode() {
            return HashCode.Combine(End, Node);
        }

        public LaneEnd GetLaneEnd(int idx) => new LaneEnd(End, Node.Lanes[idx]);

        public static bool operator ==(RoadNodeEnd left, RoadNodeEnd right) {
            return left.Equals(right);
        }

        public static bool operator !=(RoadNodeEnd left, RoadNodeEnd right) {
            return !(left == right);
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
        private NodePosition _position; // Backing field for the position
        public NodePosition PositionData {
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

        //Indexing component for the road node, maintained by the World class
        internal ISet<RoadStrip> connections = new HashSet<RoadStrip>(); // Connections to other road segments
        public ISet<RoadStrip> Connections => new ReadOnlySet<RoadStrip>(connections); // Expose the connections set

        //Halves of this road node
        public RoadNodeEnd rear => new RoadNodeEnd(this, NodeEnd.Backward);
        public RoadNodeEnd front => new RoadNodeEnd(this, NodeEnd.Forward);

        // Constructor to initialize the RoadNode with a unique ID, name, position, and world
        public RoadNode(World world, string name, Vector3 position, int azimuth, float inclination = 0, float tilt = 0, float hCurvature = 0, float vCurvature = 0, float tiltCurvature = 0) {
            Id = _nextId++;
            Name = name;
            PositionData = new NodePosition(position, azimuth, inclination, tilt, hCurvature, vCurvature, tiltCurvature);
            World = world;
        }
        public RoadNode(World world, string name, NodePosition positionData) {
            Id = _nextId++;
            Name = name;
            PositionData = positionData; // Set the position data
            World = world;
        }
    }
}
