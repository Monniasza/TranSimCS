using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS {
    internal static class Roads {

    }

    public class NodePositionChangedEventArgs : EventArgs {
        public NodePosition OldPosition { get; }
        public NodePosition NewPosition { get; }
        public NodePositionChangedEventArgs(NodePosition oldPosition, NodePosition newPosition) {
            OldPosition = oldPosition;
            NewPosition = newPosition;
        }
    }

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

        public NodePosition(Vector3 position, int azimuth, float inclination = 0f, float tilt = 0f, float hCurvature = 0, float vCurvature = 0, float tiltCurvature = 0) {
            Position = position;
            Azimuth = azimuth;
            Inclination = inclination;
            Tilt = tilt;
            HCurvature = hCurvature; // Horizontal curvature in rad/meter clockwise
            VCurvature = vCurvature;
            TiltCurvature = tiltCurvature; // Tilt curvature in rad/meter
        }

        public override bool Equals(object? obj) {
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
                }
            }
        }
        public Vector3 Position { get => PositionData.Position; set {
            var positionData = PositionData; // Create a copy of the current position data
            positionData.Position = value;
            PositionData = positionData;
        } }
        public int Azimuth { get => PositionData.Azimuth; set {
            var positionData = PositionData; // Create a copy of the current position data
            positionData.Azimuth = value; // Set the azimuth angle
            PositionData = positionData; // Update the position data
        } } // Azimuth angle in the 2^32 field
        public float Inclination { get => PositionData.Inclination; set {
            var positionData = PositionData; // Create a copy of the current position data
            positionData.Inclination = value;
            PositionData = positionData; // Update the position data
        } } // Inclination angle in radians, default is 0 (flat)
        public float Tilt { get => PositionData.Tilt; set {
            var positionData = PositionData; // Create a copy of the current position data
            positionData.Tilt = value;
            PositionData = positionData; // Update the position data
        } } // Tilt angle in radians, default is 0 (no tilt)
        public float HCurvature { get => PositionData.HCurvature; set {
            var positionData = PositionData; // Create a copy of the current position data
            positionData.HCurvature = value;
            PositionData = positionData; // Update the position data
        } } // Curvature of the road node in rad/meter clockwise, default is 0 (straight)
        public float VCurvature { get => PositionData.VCurvature; set {
            var positionData = PositionData; // Create a copy of the current position data
            positionData.VCurvature = value;
            PositionData = positionData; // Update the position data
        } } // Vertical curvature of the road node in rad/meter upwards, default is 0 (flat)
        public float TiltCurvature { get => PositionData.TiltCurvature; set {
            var positionData = PositionData; // Create a copy of the current position data
            positionData.TiltCurvature = value;
            PositionData = positionData; // Update the position data
        } } // Tilt curvature of the road node in rad/meter, default is 0 (no tilt curvature)

        public List<float> PositionOffsets { get; } = new List<float>();
        public List<LaneSpec> LaneSpecs { get; } = new List<LaneSpec>();

        // Constructor to initialize the RoadNode with a unique ID, name, position, and world
        public RoadNode(World world, string name, Vector3 position, int azimuth, float inclination = 0, float tilt = 0, float hCurvature = 0, float vCurvature = 0, float tiltCurvature = 0) {
            Id = _nextId++;
            Name = name;
            Position = position;
            World = world;
            Azimuth = azimuth;
            Inclination = inclination; // Inclination angle in radians, default is 0 (flat)
            Tilt = tilt; // Tilt angle in radians, default is 0 (no tilt)
            HCurvature = hCurvature; // Curvature of the road node in rad/meter clockwise, default is 0 (straight)
            VCurvature = vCurvature;
            TiltCurvature = tiltCurvature; // Tilt curvature of the road node in rad/meter, default is 0 (no tilt curvature)
        }
        public RoadNode(World world, string name, NodePosition positionData) {
            Id = _nextId++;
            Name = name;
            PositionData = positionData; // Set the position data
            World = world;
        }
    }

    public struct LaneConnectionSpec {
        public RoadNode StartNode; // Start node of the lane connection
        public RoadNode EndNode; // End node of the lane connection
        public int LeftStartIndex; // Lane index at the start node for the first road segment
        public int RightStartIndex; // Lane index at the end node for the first road segment
        public int LeftEndIndex; // Lane index at the start node for the second road segment
        public int RightEndIndex; // Lane index at the end node for the second road segment
        public int StartShift; // How many lanes are to the left of the lane connection from the start node
        public int EndShift; // How many lanes are to the left of the lane connection from the end node
        public LaneSpec LaneSpec = LaneSpec.Default; // Lane specification for the connection

        public LaneConnectionSpec() {}

        public LaneConnectionSpec(RoadNode startNode, RoadNode endNode, int leftStartIndex, int rightStartIndex, int leftEndIndex, int rightEndIndex, int startShift = 0, int endShift = 0, LaneSpec laneSpec = default) {
            StartNode = startNode;
            EndNode = endNode;
            LeftStartIndex = leftStartIndex;
            RightStartIndex = rightStartIndex;
            LeftEndIndex = leftEndIndex;
            RightEndIndex = rightEndIndex;
            StartShift = startShift; // How many lanes are to the left of the lane connection from the start node
            EndShift = endShift; // How many lanes are to the left of the lane connection from the end node
            LaneSpec = laneSpec; // Lane specification for the connection
        }

        public override bool Equals(object? obj) {
            if (obj is LaneConnectionSpec other) {
                return StartNode == other.StartNode &&
                       EndNode == other.EndNode &&
                       LeftStartIndex == other.LeftStartIndex &&
                       RightStartIndex == other.RightStartIndex &&
                       LeftEndIndex == other.LeftEndIndex &&
                       RightEndIndex == other.RightEndIndex &&
                       StartShift == other.StartShift &&
                       EndShift == other.EndShift &&
                       LaneSpec.Equals(other.LaneSpec);
            }
            return false;
        }

        public override int GetHashCode() {
            HashCode hash = new HashCode();
            hash.Add(StartNode);
            hash.Add(EndNode);
            hash.Add(LeftStartIndex);
            hash.Add(RightStartIndex);
            hash.Add(LeftEndIndex);
            hash.Add(RightEndIndex);
            hash.Add(StartShift);
            hash.Add(EndShift);
            hash.Add(LaneSpec);
            return hash.ToHashCode(); // Generate a hash code based on the properties of the lane connection specification
        }
        public static bool operator ==(LaneConnectionSpec left, LaneConnectionSpec right) {
            return left.Equals(right);
        }

        public static bool operator !=(LaneConnectionSpec left, LaneConnectionSpec right) {
            return !(left == right);
        }
    }


    public class LaneConnectionChangedEventArgs(LaneConnectionSpec oldPosition, LaneConnectionSpec newPosition) : EventArgs {
        public LaneConnectionSpec OldPosition { get; } = oldPosition;
        public LaneConnectionSpec NewPosition { get; } = newPosition;
    }
    /// <summary>
    /// Represents a connection between two road nodes, including lane indices and specifications.
    /// </summary>
    /// <remarks>A <see cref="LaneConnection"/> defines the relationship between two road nodes, specifying
    /// the lanes involved at each node and their respective indices. It also includes properties for lane
    /// specifications and rendering-related data, such as meshes for visualization.</remarks>
    public class LaneConnection {
        // Properties to hold the start and end nodes and their respective lane indices
        private LaneConnectionSpec _spec; // Backing field for the lane connection specification
        public event EventHandler<LaneConnectionChangedEventArgs> SpecChanged; // Event to notify when the specification changes
        public LaneConnectionSpec Spec { get => _spec; set {
            var oldSpec = _spec; // Create a copy of the current specification
            if (oldSpec.Equals(value)) return; // If the new specification is the same as the old one, do nothing
            Mesh = null; // Invalidate the mesh when the specification changes
            SpecChanged?.Invoke(this, new LaneConnectionChangedEventArgs(oldSpec, value)); // Raise the event with old and new specification
            _spec = value; // Set the new specification
        } } // Specification for the lane connection


        //Properties for the first node
        public RoadNode StartNode { get => Spec.StartNode; set { 
            var spec = Spec; // Create a copy of the current specification
            spec.StartNode = value; // Set the start node
            Spec = spec; // Update the specification
        } }
        public int LeftStartIndex { get => Spec.LeftStartIndex; set {
            var spec = Spec; // Create a copy of the current specification
            spec.LeftStartIndex = value; // Set the start node
            Spec = spec; // Update the specification
        } } // Lane index at the start node for the first road segment
        public int RightStartIndex { get => Spec.RightStartIndex; set {
            var spec = Spec; // Create a copy of the current specification
            spec.RightStartIndex = value; // Set the start node
            Spec = spec; // Update the specification
        } } // Lane index at the end node for the first road segment
        public int StartShift { get => Spec.StartShift; set {
            var spec = Spec; // Create a copy of the current specification
            spec.StartShift = value; // Set the start node
            Spec = spec; // Update the specification
        } } // How many lanes are to the left of the lane connection from the start node

        //Properties for the second node
        public RoadNode EndNode { get => Spec.EndNode; set {
            var spec = Spec; // Create a copy of the current specification
            spec.EndNode = value; // Set the start node
            Spec = spec; // Update the specification
        } }
        public int LeftEndIndex { get => Spec.LeftEndIndex; set {
            var spec = Spec; // Create a copy of the current specification
            spec.LeftEndIndex = value; // Set the start node
            Spec = spec; // Update the specification
        } } // Lane index at the start node for the second road segment
        public int RightEndIndex { get => Spec.RightEndIndex; set {
            var spec = Spec; // Create a copy of the current specification
            spec.RightEndIndex = value; // Set the start node
            Spec = spec; // Update the specification
        } } // Lane index at the end node for the second road segment      
        public int EndShift { get => Spec.EndShift; set {
            var spec = Spec; // Create a copy of the current specification
            spec.EndShift = value; // Set the start node
            Spec = spec; // Update the specification
        } } // How many lanes are to the left of the lane connection from the end node

        //Lane specification for the connection
        public LaneSpec LaneSpec { get => Spec.LaneSpec; set {
            var spec = Spec; // Create a copy of the current specification
            spec.LaneSpec = value; // Set the lane specification
            Spec = spec;
        } } // Lane specification for the connection

        public LaneTag FullSizeTag() {
            return new LaneTag(this, LeftStartIndex, RightStartIndex, LeftEndIndex, RightEndIndex, LaneSpec); // Create a LaneTag with the full size of the connection
        }

        //Meshes for the lane connection (can be used for rendering and cached)
        private Mesh? _endMesh; // Mesh for the lane connection at the end node
        public Mesh? Mesh { get { 
            if (_endMesh != null) return _endMesh; // If the end mesh is set, return it
            _endMesh = new Mesh();
            RoadRenderer.RenderRoadSegment(this, _endMesh); // Otherwise, render the road segment
            return _endMesh; // Return the rendered mesh
        } private set => _endMesh = value; } // Mesh for the lane connection at the start node
        internal void InvalidateMesh() {
            _endMesh = null; // Invalidate the mesh, forcing it to be re-rendered
        }


        // Constructor to initialize the LaneConnection with start and end nodes and their lane indices
        public LaneConnection(RoadNode node1, RoadNode node2, int lsi, int rsi, int lei, int rei, int ssh = 0, int esh = 0) {
            StartNode = node1;
            EndNode = node2;
            LeftStartIndex = lsi;
            RightStartIndex = rsi;
            LeftEndIndex = lei;
            RightEndIndex = rei;
            StartShift = ssh; // How many lanes are to the left of the lane connection from the start node
            EndShift = esh;
        }
    }

    public struct LaneSpec {
        public Color Color { get; set; } // Color of the lane
        public VehicleTypes VehicleTypes { get; set; } // Types of vehicles allowed in the lane
        public LaneFlags Flags { get; set; } // Flags for additional lane properties
        // Constructor to initialize the LaneSpec with lane index, width, and offset
        public LaneSpec(Color color, VehicleTypes vehicleTypes) {
            Color = color;
            VehicleTypes = vehicleTypes;
        }

        //Common presets for lane specifications
        public static LaneSpec Default => new(Color.Gray, VehicleTypes.Vehicles);
        public static LaneSpec Bicycle => new(Color.Green, VehicleTypes.Bicycle);
        public static LaneSpec Pedestrian => new(Color.Blue, VehicleTypes.Pedestrian);
        public static LaneSpec Bus => new(Color.Orange, VehicleTypes.Bus);
        public static LaneSpec Truck => new(Color.Brown, VehicleTypes.Truck);
        public static LaneSpec Car => new(Color.Red, VehicleTypes.Car);
        public static LaneSpec None => new(Color.Transparent, VehicleTypes.None);
        public static LaneSpec All => new(Color.White, VehicleTypes.All); // All vehicle types allowed
    }

    public struct LaneTag {
        public LaneConnection road; // The road connection this tag is associated with
        public int startLaneIndexL; // The starting lane index for the tag
        public int startLaneIndexR;
        public int endLaneIndexL;
        public int endLaneIndexR;
        public LaneSpec laneSpec; // The lane specification for the tag
        public LaneTag(LaneConnection road, int startLaneIndexL, int startLaneIndexR, int endLaneIndexL, int endLaneIndexR, LaneSpec laneSpec) {
            this.road = road;
            this.startLaneIndexL = startLaneIndexL;
            this.startLaneIndexR = startLaneIndexR;
            this.endLaneIndexL = endLaneIndexL;
            this.endLaneIndexR = endLaneIndexR;
            this.laneSpec = laneSpec;
        }
    }

    [Flags]
    public enum VehicleTypes {
        None = 0,
        Car = 1,
        Truck = 2,
        Bus = 4,
        Bicycle = 8,
        Pedestrian = 16,

        // Composite types for convenience  
        Path = Bicycle | Pedestrian,
        MotorVehicles = Car | Truck | Bus,
        Vehicles = MotorVehicles | Bicycle, // All vehicles except parking
        Transport = Vehicles | Pedestrian, // All traffic
        All = -1 // All vehicle and parking types
    }

    [Flags]
    public enum LaneFlags {
        None = 0,
        Forward = 1, // Lane is for forward traffic
        Backward = 2, // Lane is for backward traffic
        LeftTurn = 4, // Lane is for left turns
        RightTurn = 8, // Lane is for right turns
        Straight = 16, // Lane is for straight traffic
        SwitchLeft = 32, // Lane is for switching to the left
        SwitchRight = 64,
        Parking = 128, // Lane is for parking
        Median = 256, // Lane is a median
        Planting = 512, // Lane is for planting
        Platform = 1024, // Lane is a platform (e.g., for buses or trams)
    }
}
