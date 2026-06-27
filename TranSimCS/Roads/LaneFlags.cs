using System;

namespace TranSimCS.Roads {
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
        /// Allow vehicles to switch to the next lane to the right in the strip's direction. Does not apply to rail.
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
