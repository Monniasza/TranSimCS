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
        Dashed = 0,
        /// <summary>
        /// Indicates that the road spline is a solid line. It can be cut by driveable areas
        /// </summary>
        Solid = 1,
        /// <summary>
        /// Indicates a piece of asphalt. It cuts solid lines
        /// </summary>
        Asphalt = 2,
        /// <summary>
        /// Number of distinct <see cref="RoadSplineComponentType"/>s. It is not a valid value.
        /// </summary>
        Count = 3
    }
    public static class RoadSplineComponentTypeMethods {
        public static SimpleMaterial GetMaterial(this RoadSplineComponentType type) {
            return type switch {
                RoadSplineComponentType.Dashed => Assets.LineDash,
                RoadSplineComponentType.Solid => Assets.EmissiveWhite,
                RoadSplineComponentType.Asphalt => Assets.Asphalt,
                _ => throw new ArgumentException($"Invalid road spline component type: {type}"),
            };
        }
    }
    public struct RoadSplineComponent {
        public SplineStrip Strip;
        public Color Color;
        public RoadSplineComponentType Type;
        public float Bias;
    }
}
