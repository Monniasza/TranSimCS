using System;

namespace TranSimCS.Roads {
    [Flags]
    public enum LaneFlags {
        //RESERVED
        R1 = 1,
        R2 = 4,
        R3 = 8,
        R4 = 16,

        /// <summary>
        /// No flags set
        /// </summary>
        None = 0,
        /// <summary>
        /// Prohibit vehicles from switching to the next lane to the left in the strip's direction. Does not apply to rail.
        /// </summary>
        NoLeft = 32,
        /// <summary>
        /// Prohibit vehicles from switching to the next lane to the right in the strip's direction. Does not apply to rail.
        /// </summary>
        NoRight = 64,
        /// <summary>
        /// Should vehicles be allowed to back off/drive the wrong way? If yes, the lane will be semi-bidirectional, with the normal direction preferred and opposite the secondary.
        /// </summary>
        AllowReverse = 2,
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
        /// <summary>
        /// Does this lane serve public transit and/or hitchhikers? Pedestrian lane strips only.
        /// </summary>
        Platform = 1024, // Lane is a platform (e.g., for buses or trams)
    }
}
