using System;
using Microsoft.Xna.Framework;

namespace TranSimCS.Roads {

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

        public LaneConnectionSpec() { }

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

        public override bool Equals(object obj) {
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
        private Mesh _endMesh; // Mesh for the lane connection at the end node
        public Mesh Mesh { get { 
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
            LaneSpec = LaneSpec.Default; // Default lane specification
        }
    }
}
