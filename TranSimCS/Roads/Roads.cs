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

    [Flags]
    public enum LaneFlags {

        /// <summary>
        /// No flags set
        /// </summary>
        None = 0,

        /// <summary>
        /// Enabled if the traffic is supposed to go backwards normally. 
        /// If the object is a lane, the forward direction is rear-end to front-end and vice-versa.
        /// If the object is a strip/segment, the forward direction is start-node to end-node and vice-versa.
        /// If the object is a section, the forward direction is inwards and the back direction is outwards.
        /// </summary>
        IsBackward = 1,
        /// <summary>
        /// Should vehicles be allowed to back off/drive the wrong way? If yes, the lane will be semi-bidirectional, with the normal direction preferred and opposite the secondary.
        /// </summary>
        AllowReverse = 2,


        LeftTurn = 4, // Lane is for left turns
        RightTurn = 8, // Lane is for right turns
        Straight = 16, // Lane is for straight traffic

        /// <summary>
        /// Allow vehicles to switch to the next lane to the left in the strip's direction. Does not apply to rail.
        /// </summary>
        SwitchLeft = 32,
        /// <summary>
        /// Allow vehicles to switch to the next lane to the lright in the strip's direction. Does not apply to rail.
        /// </summary>
        SwitchRight = 64,

        /// <summary>
        /// Is the lane meant for stopped vehicles? If yes, the vehicles will park there and through traffic will avoid it.
        /// </summary>
        Parking = 128, // Lane is for parking

        /// <summary>
        /// Will the vehicles yield to oncoming traffic? Nodes only.
        /// </summary>
        Yield = 256,
        /// <summary>
        /// Will all vehicles stop on the line? Nodes only.
        /// </summary>
        Stop = 512,


        Platform = 1024, // Lane is a platform (e.g., for buses or trams)
    }
}
