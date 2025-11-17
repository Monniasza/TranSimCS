using System;
using Microsoft.Xna.Framework;

namespace TranSimCS.Roads {
    public struct LaneSpec : IEquatable<LaneSpec> {
        public Color Color { get; set; } // Color of the lane
        public VehicleTypes VehicleTypes { get; set; } // Types of vehicles allowed in the lane
        public LaneFlags Flags { get; set; } // Flags for additional lane properties
        public float Width { get; set; } //Width. Ignored by nodes, but used to store new lane widths
        public float SpeedLimit { get; set; } //Speed limit [km/h]


        // Constructor to initialize the LaneSpec with lane index, width, and offset
        public LaneSpec(Color color, VehicleTypes vehicleTypes, float width = 3.5f, float speedLimit = 50, LaneFlags flags = LaneFlags.None) {
            Color = color;
            VehicleTypes = vehicleTypes;
            Flags = flags;
            Width = width;
            SpeedLimit = speedLimit;
        }

        //Common presets for lane specifications
        public static LaneSpec Default => new(Color.Gray, VehicleTypes.Vehicles, 3f, 50);
        public static LaneSpec Motorway => new(Color.DarkGray, VehicleTypes.MotorVehicles, 3.5f, 150);
        public static LaneSpec Bicycle => new(Color.Green, VehicleTypes.Bicycle, 2, 30);
        public static LaneSpec Pedestrian => new(Color.LightGray, VehicleTypes.Pedestrian, 1.5f, 16);
        public static LaneSpec Path => new(Color.LightGray, VehicleTypes.Path, 3,20);
        public static LaneSpec Bus => new(Color.Red, VehicleTypes.Bus, 3, 80);
        public static LaneSpec None => new(Color.Transparent, VehicleTypes.None, 3, 0);
        public static LaneSpec All => new(Color.White, VehicleTypes.All, 3, 100); // All vehicle types allowed
        public static LaneSpec Platform => new(Color.LightGoldenrodYellow, VehicleTypes.Pedestrian, 3, 10, LaneFlags.Platform);

        public override bool Equals(object? obj) {
            return obj is LaneSpec spec && Equals(spec);
        }

        public bool Equals(LaneSpec other) {
            return Color.Equals(other.Color) &&
                   VehicleTypes == other.VehicleTypes &&
                   Flags == other.Flags &&
                   Width == other.Width &&
                   SpeedLimit == other.SpeedLimit;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Color, VehicleTypes, Flags, Width, SpeedLimit);
        }

        public static bool operator ==(LaneSpec left, LaneSpec right) {
            return left.Equals(right);
        }

        public static bool operator !=(LaneSpec left, LaneSpec right) {
            return !(left == right);
        }
    }
}
