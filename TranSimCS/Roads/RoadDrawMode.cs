using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Roads {
    [Flags]
    public enum RoadDrawMode {
        /// <summary>
        /// Draws no elements
        /// </summary>
        None = 0,
        /// <summary>
        /// Draws signs
        /// </summary>
        Signs = 1,
        /// <summary>
        /// Draws lines
        /// </summary>
        Lines = 2,
        /// <summary>
        /// Draws islands
        /// </summary>
        Islands = 4,
        /// <summary>
        /// Draws road finishes
        /// </summary>
        Finish = 8,

        /// <summary>
        /// Draws all elements
        /// </summary>
        All = -1

    }
}
