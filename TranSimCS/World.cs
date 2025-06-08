using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS
{
    public class World
    {
        public List<RoadSegment> RoadSegments { get; } = new List<RoadSegment>();
        public List<RoadNode> RoadNodes { get; } = new List<RoadNode>();
    }

    public class RoadNode
    {
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

        public List<float> PositionOffsets { get; } = new List<float>();
        public List<LaneSpec> LaneSpecs { get; } = new List<LaneSpec>();

        // Constructor to initialize the RoadNode with a unique ID, name, position, and world
        public RoadNode(World world, string name, Vector3 position, int azimuth)
        {
            Id = _nextId++;
            Name = name;
            Position = position;
            World = world;
            Azimuth = azimuth;
        }
    }

    public enum RoadNodeType
    {
        Normal,
        Start,
        End,
        Intersection
    }

    public struct LaneConnection
    {
        // Properties to hold the start and end nodes and their respective lane indices
        //Properties for the first node
        public RoadNode StartNode { get; init; }
        public int LeftStartIndex { get; init; } // Lane index at the start node for the first road segment
        public int RightStartIndex { get; init; } // Lane index at the end node for the first road segment
        public int StartShift { get; set; } // How many lanes are to the left of the lane connection from the start node

        //Properties for the second node
        public RoadNode EndNode { get; init; }
        public int LeftEndIndex { get; init; } // Lane index at the start node for the second road segment
        public int RightEndIndex { get; init; } // Lane index at the end node for the second road segment      
        public int EndShift { get; set; } // How many lanes are to the left of the lane connection from the end node

        //Properties for the lane connection
        public LaneSpec LaneSpec { get; set; } = LaneSpec.Default; // Lane specification for the connection

        // Constructor to initialize the LaneConnection with start and end nodes and their lane indices
        public LaneConnection(RoadNode node1, RoadNode node2, int lsi, int rsi, int lei, int rei)
        {
            StartNode = node1;
            EndNode = node2;
            LeftStartIndex = lsi;
            RightStartIndex = rsi;
            LeftEndIndex = lei;
            RightEndIndex = rei;
        }
    }

    public struct LaneSpec {
        public Color Color { get; set; } // Color of the lane
        public VehicleTypes VehicleTypes { get; set; } // Types of vehicles allowed in the lane
        // Constructor to initialize the LaneSpec with lane index, width, and offset
        public LaneSpec(Color color, VehicleTypes vehicleTypes)
        {
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

    [Flags]
    public enum VehicleTypes
    {
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

    public class RoadSegment
    {
        public List<RoadNode> Nodes { get; } = new List<RoadNode>();
        public World World { get; init; }
        public List<LaneConnection> LaneConnections { get; } = [];
        // Constructor to initialize the RoadSegment with start and end nodes and the world
        public RoadSegment(World world, List<RoadNode> nodes)
        {
            World = world;
            Nodes.AddRange(nodes);
        }
        public RoadSegment(World world, params RoadNode[] nodes)
        {
            World = world;
            Nodes.AddRange(nodes);
        }
    }
}
