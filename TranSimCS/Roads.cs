using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS {
    internal static class Roads {

    }
    public class RoadNode {
        //Example azimuth values
        public const int AZIMUTH_NORTH = 0; // 0 degrees
        public const int AZIMUTH_EAST = 1 << 30; // 90 degrees
        public const int AZIMUTH_SOUTH = 2 << 30; // 180 degrees
        public const int AZIMUTH_WEST = 3 << 30; // 270 degrees

        private static int _nextId = 1; // Static field to keep track of the next ID
        public int Id { get; init; }
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public World World { get; init; }
        public int Azimuth { get; set; } // Azimuth angle in the 2^32 field
        public float Curvature { get; set; } = 0f; // Curvature of the road node in rad/meter clockwise, default is 0 (straight)

        public List<float> PositionOffsets { get; } = new List<float>();
        public List<LaneSpec> LaneSpecs { get; } = new List<LaneSpec>();

        // Constructor to initialize the RoadNode with a unique ID, name, position, and world
        public RoadNode(World world, string name, Vector3 position, int azimuth) {
            Id = _nextId++;
            Name = name;
            Position = position;
            World = world;
            Azimuth = azimuth;
        }
    }

    public enum RoadNodeType {
        Normal,
        Start,
        End,
        Intersection
    }

    public class LaneConnection {
        // Properties to hold the start and end nodes and their respective lane indices
        //Properties for the first node
        public RoadNode StartNode { get; init; }
        public int LeftStartIndex { get; init; } // Lane index at the start node for the first road segment
        public int RightStartIndex { get; init; } // Lane index at the end node for the first road segment
        public int StartShift { get; init; } // How many lanes are to the left of the lane connection from the start node

        //Properties for the second node
        public RoadNode EndNode { get; init; }
        public int LeftEndIndex { get; init; } // Lane index at the start node for the second road segment
        public int RightEndIndex { get; init; } // Lane index at the end node for the second road segment      
        public int EndShift { get; init; } // How many lanes are to the left of the lane connection from the end node

        public LaneTag FullSizeTag() {
            return new LaneTag(this, LeftStartIndex, RightStartIndex, LeftEndIndex, RightEndIndex, LaneSpec); // Create a LaneTag with the full size of the connection
        }

        //Meshes for the lane connection (can be used for rendering and cached)
        private Mesh? _endMesh; // Mesh for the lane connection at the end node
        public Mesh? StartMesh { get { 
            if (_endMesh != null) return _endMesh; // If the end mesh is set, return it
            _endMesh = new Mesh();
            RoadRenderer.RenderRoadSegment(this, _endMesh); // Otherwise, render the road segment
            return _endMesh; // Return the rendered mesh
        } private set => _endMesh = value; } // Mesh for the lane connection at the start node

        //Properties for the lane connection
        private LaneSpec LaneSpec0 = LaneSpec.Default ; // Backing field for the lane specification
        public LaneSpec LaneSpec { get => LaneSpec0; set { 
            StartMesh = null; // Reset the mesh when the lane specification changes
            LaneSpec0 = value;
        } } // Lane specification for the connection

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
        // Constructor to initialize the LaneSpec with lane index, width, and offset
        public LaneSpec(Color color, VehicleTypes vehicleTypes) {
            Color = color;
            VehicleTypes = vehicleTypes;
        }

        //Common presets for lane specifications
        public static LaneSpec Default => new(Color.Gray, VehicleTypes.Vehicles);
        public static LaneSpec Bicycle => new(Color.Green, VehicleTypes.Bicycle);
        public static LaneSpec Pedestrian => new(Color.Blue, VehicleTypes.Pedestrian);
        public static LaneSpec Parking => new(Color.Yellow, VehicleTypes.Parking);
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
        Parking = 32, // Parking spaces

        // Composite types for convenience  
        Path = Bicycle | Pedestrian,
        MotorVehicles = Car | Truck | Bus,
        Vehicles = MotorVehicles | Bicycle, // All vehicles except parking
        Transport = Vehicles | Pedestrian, // All traffic
        All = -1 // All vehicle and parking types
    }
}
