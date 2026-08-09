using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.ModelOld;
using TranSimCS.Spline;

namespace TranSimCS.Roads.Strip {
    public enum RoadSplineComponentType {
        /// <summary>
        /// Indicates that the road spline is a dashed line. It should not be cut.
        /// </summary>
        UnclippedMarking = 0,
        /// <summary>
        /// Indicates that the road spline is a solid line. It can be cut by driveable areas
        /// </summary>
        ClippedMarking = 1,
        /// <summary>
        /// Indicates a piece of asphalt. It cuts solid lines
        /// </summary>
        RoadSurface = 2,
        /// <summary>
        /// Indicates a drivable area strip. Cuts out solid lines.
        /// </summary>
        MarkingClip = 3,
        /// <summary>
        /// Number of distinct <see cref="RoadSplineComponentType"/>s. It is not a valid value.
        /// </summary>
        Count = 4,
    }
    public struct RoadSplineComponent {
        public Color Color;
        public RoadSplineComponentType Type;
        public SimpleMaterial? Texture;
        public float Bias;
    }
}
