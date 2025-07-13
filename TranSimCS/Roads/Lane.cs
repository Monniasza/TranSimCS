using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TranSimCS.Roads {
    public struct LaneSpec {
        public Color Color { get; set; } // Color of the lane
        public VehicleTypes VehicleTypes { get; set; } // Types of vehicles allowed in the lane
        public LaneFlags Flags { get; set; } // Flags for additional lane properties
        public float Width { get; set; } //Width. Ignored by nodes, but used to store new lane widths
        public float SpeedLimit { get; set; } //Speed limit


        // Constructor to initialize the LaneSpec with lane index, width, and offset
        public LaneSpec(Color color, VehicleTypes vehicleTypes, LaneFlags flags = LaneFlags.Forward) {
            Color = color;
            VehicleTypes = vehicleTypes;
            Flags = flags;
        }

        //Common presets for lane specifications
        public static LaneSpec Default => new(Color.Gray, VehicleTypes.Vehicles);
        public static LaneSpec Motorway => new(Color.DarkGray, VehicleTypes.MotorVehicles);
        public static LaneSpec Bicycle => new(Color.Green, VehicleTypes.Bicycle);
        public static LaneSpec Pedestrian => new(Color.LightGray, VehicleTypes.Pedestrian);
        public static LaneSpec Path => new(Color.LightGray, VehicleTypes.Path);
        public static LaneSpec Bus => new(Color.Red, VehicleTypes.Bus);
        public static LaneSpec None => new(Color.Transparent, VehicleTypes.None);
        public static LaneSpec All => new(Color.White, VehicleTypes.All); // All vehicle types allowed
        public static LaneSpec Platform => new(Color.LightGoldenrodYellow, VehicleTypes.Pedestrian, LaneFlags.Platform);
    }

    public struct LaneEnd(NodeEnd End, Lane Lane) : IEquatable<LaneEnd> {
        public NodeEnd end = End;
        public Lane lane = Lane;

        public RoadNodeEnd RoadNodeEnd => lane.RoadNode.GetEnd(end);

        public LaneEnd OppositeEnd => new LaneEnd(end.Negate(), lane);

        public override bool Equals(object obj) {
            return obj is LaneEnd end && Equals(end);
        }

        public bool Equals(LaneEnd other) {
            return end == other.end &&
                   EqualityComparer<Lane>.Default.Equals(lane, other.lane);
        }

        public override int GetHashCode() {
            return HashCode.Combine(end, lane);
        }

        public static bool operator ==(LaneEnd left, LaneEnd right) {
            return left.Equals(right);
        }

        public static bool operator !=(LaneEnd left, LaneEnd right) {
            return !(left == right);
        }
    }

    public class Lane(RoadNode node) {
        public RoadNode RoadNode => node; // Reference to the road node this lane belongs to
        private LaneSpec _spec;
        /// <summary>
        /// Specification of the lane, including properties like color, type, etc.
        /// The width here is ignored when set, but it's returned with the proper value when get.
        /// </summary>
        public LaneSpec Spec { get {
            _spec.Width = Width;
            return _spec;
        } set => _spec = value; } 
        public float LeftPosition { get; set; } // Left position of the lane relative to the road node
        public float RightPosition { get; set; } // Right position of the lane relative to the road node
        public int Index { get; internal set; } // Index of the lane in the road node's lane list
        public float MiddlePosition => (LeftPosition + RightPosition) / 2; // Middle position of the lane, calculated as the average of left and right positions
        public float Width => RightPosition - LeftPosition;
        public LaneEnd Rear => new LaneEnd(NodeEnd.Backward, this);
        public LaneEnd Front => new LaneEnd(NodeEnd.Forward, this);

        //Indexing
        internal ISet<LaneStrip> connections = new HashSet<LaneStrip>(); // Set of lane strips that this lane is connected to
        public ISet<LaneStrip> Connections => new ReadOnlySet<LaneStrip>(connections); // Read-only set of lane strips that this lane is connected to
    }
}
