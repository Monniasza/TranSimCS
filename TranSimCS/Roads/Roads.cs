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
        MotorVehicles = Car | Truck | Bus,

        //Non-motorized vehicles
        Bicycle = 8,
        Pedestrian = 16,
        Horse = 128,
        Path = Bicycle | Pedestrian | Horse,

        //Aircraft
        Plane = 256,
        Rocket = 512,
        Aircraft = Plane | Rocket,

        //Railways
        LRT = 32,
        Train = 64,
        Rail = LRT | Train,

        // Composite types for convenience  
        Vehicles = MotorVehicles | Bicycle | Horse, // All vehicles except parking
        Transport = Vehicles | Pedestrian, // All traffic
        All = -1 // All vehicle and parking types
    }
}
