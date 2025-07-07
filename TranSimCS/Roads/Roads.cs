using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Roads {
    internal static class Roads {

    }

    [Flags]
    public enum VehicleTypes {
        None = 0,

        //Motor vehicles
        Car = 1,
        Truck = 2,
        Bus = 4,

        //Non-motorized vehicles
        Bicycle = 8,
        Pedestrian = 16,
        Horse = 128,

        //Aircraft
        Plane = 256,
        Rocket = 512,

        //Railways
        LRT = 32,
        Train = 64,


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
